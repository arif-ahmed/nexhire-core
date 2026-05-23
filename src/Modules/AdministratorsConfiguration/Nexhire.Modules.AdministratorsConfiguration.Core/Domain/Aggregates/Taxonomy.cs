using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Entities;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Events;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Aggregates;

public sealed class Taxonomy : AggregateRoot<Guid>
{
    private readonly List<TaxonomyTerm> _terms = new();

    public TaxonomyKind Kind { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public IReadOnlyCollection<TaxonomyTerm> Terms => _terms.AsReadOnly();
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private Taxonomy() : base()
    {
        // Required by EF Core
    }

    private Taxonomy(Guid id, TaxonomyKind kind, string name) : base(id)
    {
        Kind = kind;
        Name = name;
        Version = 1;
        CreatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public static Taxonomy Create(TaxonomyKind kind, string name)
    {
        var taxonomy = new Taxonomy(Guid.NewGuid(), kind, name);
        taxonomy.RaiseDomainEvent(new TaxonomyCreatedDomainEvent(taxonomy.Id, kind, DateTime.UtcNow));
        return taxonomy;
    }

    public Result AddTerm(TermCode code, string label, SkillCategory? category, TermCode? parentCode)
    {
        // Invariant 1: Prefix validation
        var expectedPrefix = GetExpectedPrefix(Kind);
        if (!code.Value.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            return Result.Failure(new Error("E-TAXO-CODE-PREFIX-MISMATCH", $"Term code must start with prefix '{expectedPrefix}' for {Kind} taxonomy."));
        }

        // Invariant 2: Duplicate term code check
        if (_terms.Any(t => t.Code == code))
        {
            return Result.Failure(new Error("E-TAXO-DUPLICATE-CODE", $"A term with code '{code.Value}' already exists in this taxonomy."));
        }

        // Invariant 3: Skill Category rules
        if (Kind == TaxonomyKind.Skills)
        {
            if (category == null)
            {
                return Result.Failure(new Error("E-TAXO-CATEGORY-REQUIRED", "Skill category is required for Skills taxonomy."));
            }
        }
        else
        {
            if (category != null)
            {
                return Result.Failure(new Error("E-TAXO-INVALID-CATEGORY", "Category must be null for non-skills taxonomies."));
            }
        }

        // Invariant 4: Parent checks
        if (parentCode != null)
        {
            var parent = _terms.FirstOrDefault(t => t.Code == parentCode);
            if (parent == null)
            {
                return Result.Failure(new Error("E-TAXO-PARENT-NOT-FOUND", $"Parent term with code '{parentCode.Value}' was not found."));
            }
            if (parent.Status == TermStatus.Deprecated)
            {
                return Result.Failure(new Error("E-TAXO-PARENT-DEPRECATED", $"Cannot set deprecated term '{parentCode.Value}' as a parent."));
            }
        }

        var term = TaxonomyTerm.Create(Guid.NewGuid(), code, label, category, parentCode);
        _terms.Add(term);

        IncrementVersion();
        RaiseDomainEvent(new TaxonomyTermAddedDomainEvent(Id, Kind, code, label, category, parentCode));
        RaiseDomainEvent(new TaxonomyUpdatedDomainEvent(Id, Kind, Version, $"Added term '{code.Value}'"));

        return Result.Success();
    }

    public Result RenameTerm(TermCode code, string newLabel)
    {
        var term = _terms.FirstOrDefault(t => t.Code == code);
        if (term == null)
        {
            return Result.Failure(new Error("E-TAXO-TERM-NOT-FOUND", $"Term with code '{code.Value}' was not found."));
        }

        term.UpdateLabel(newLabel);

        IncrementVersion();
        RaiseDomainEvent(new TaxonomyUpdatedDomainEvent(Id, Kind, Version, $"Renamed term '{code.Value}' to '{newLabel}'"));

        return Result.Success();
    }

    public Result RecategorizeTerm(TermCode code, SkillCategory newCategory)
    {
        if (Kind != TaxonomyKind.Skills)
        {
            return Result.Failure(new Error("E-TAXO-INVALID-OPERATION", "Recategorization is only supported for the Skills taxonomy."));
        }

        var term = _terms.FirstOrDefault(t => t.Code == code);
        if (term == null)
        {
            return Result.Failure(new Error("E-TAXO-TERM-NOT-FOUND", $"Term with code '{code.Value}' was not found."));
        }

        term.UpdateCategory(newCategory);

        IncrementVersion();
        RaiseDomainEvent(new TaxonomyUpdatedDomainEvent(Id, Kind, Version, $"Recategorized term '{code.Value}' to '{newCategory}'"));

        return Result.Success();
    }

    public Result ReparentTerm(TermCode code, TermCode? newParentCode)
    {
        var term = _terms.FirstOrDefault(t => t.Code == code);
        if (term == null)
        {
            return Result.Failure(new Error("E-TAXO-TERM-NOT-FOUND", $"Term with code '{code.Value}' was not found."));
        }

        if (newParentCode != null)
        {
            if (newParentCode == code)
            {
                return Result.Failure(new Error("E-TAXO-CYCLE", "A term cannot be its own parent."));
            }

            var parent = _terms.FirstOrDefault(t => t.Code == newParentCode);
            if (parent == null)
            {
                return Result.Failure(new Error("E-TAXO-PARENT-NOT-FOUND", $"Parent term with code '{newParentCode.Value}' was not found."));
            }
            if (parent.Status == TermStatus.Deprecated)
            {
                return Result.Failure(new Error("E-TAXO-PARENT-DEPRECATED", $"Cannot set deprecated term '{newParentCode.Value}' as a parent."));
            }

            if (WouldCreateCycle(code, newParentCode))
            {
                return Result.Failure(new Error("E-TAXO-CYCLE", "Hierarchy change would create a cycle."));
            }
        }

        term.UpdateParent(newParentCode);

        IncrementVersion();
        RaiseDomainEvent(new TaxonomyUpdatedDomainEvent(Id, Kind, Version, $"Reparented term '{code.Value}' to {(newParentCode != null ? $"'{newParentCode.Value}'" : "null")}"));

        return Result.Success();
    }

    public Result DeprecateTerm(TermCode code, TermCode? replacedByCode)
    {
        var term = _terms.FirstOrDefault(t => t.Code == code);
        if (term == null)
        {
            return Result.Failure(new Error("E-TAXO-TERM-NOT-FOUND", $"Term with code '{code.Value}' was not found."));
        }

        if (term.Status == TermStatus.Deprecated)
        {
            return Result.Success(); // Idempotent no-op
        }

        if (replacedByCode != null)
        {
            if (replacedByCode == code)
            {
                return Result.Failure(new Error("E-TAXO-SELF-REPLACE", "A term cannot be replaced by itself."));
            }

            var replacement = _terms.FirstOrDefault(t => t.Code == replacedByCode);
            if (replacement == null)
            {
                return Result.Failure(new Error("E-TAXO-REPLACEMENT-NOT-FOUND", $"Replacement term with code '{replacedByCode.Value}' was not found in this taxonomy."));
            }
        }

        term.Deprecate(replacedByCode);

        IncrementVersion();
        RaiseDomainEvent(new TaxonomyTermDeprecatedDomainEvent(Id, Kind, code, replacedByCode));
        RaiseDomainEvent(new TaxonomyUpdatedDomainEvent(Id, Kind, Version, $"Deprecated term '{code.Value}'"));

        return Result.Success();
    }

    public Result ReactivateTerm(TermCode code)
    {
        var term = _terms.FirstOrDefault(t => t.Code == code);
        if (term == null)
        {
            return Result.Failure(new Error("E-TAXO-TERM-NOT-FOUND", $"Term with code '{code.Value}' was not found."));
        }

        if (term.Status == TermStatus.Active)
        {
            return Result.Success(); // Idempotent no-op
        }

        if (term.ParentCode != null)
        {
            var parent = _terms.FirstOrDefault(t => t.Code == term.ParentCode);
            if (parent == null || parent.Status == TermStatus.Deprecated)
            {
                return Result.Failure(new Error("E-TAXO-PARENT-DEPRECATED", "Cannot reactivate a term whose parent is deprecated or missing. Please reparent it first."));
            }
        }

        term.Reactivate();

        IncrementVersion();
        RaiseDomainEvent(new TaxonomyUpdatedDomainEvent(Id, Kind, Version, $"Reactivated term '{code.Value}'"));

        return Result.Success();
    }

    public Result ApplyUsageDelta(TermCode code, int delta)
    {
        var term = _terms.FirstOrDefault(t => t.Code == code);
        if (term == null)
        {
            return Result.Failure(new Error("E-TAXO-TERM-NOT-FOUND", $"Term with code '{code.Value}' was not found."));
        }

        term.ModifyUsageCount(delta);
        // Do NOT bump version and do NOT raise domain events for usage count updates.
        return Result.Success();
    }

    internal Result TryAddTermForImport(TermCode code, string label, SkillCategory? category, TermCode? parentCode)
    {
        var expectedPrefix = GetExpectedPrefix(Kind);
        if (!code.Value.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            return Result.Failure(new Error("E-TAXO-CODE-PREFIX-MISMATCH", $"Term code must start with prefix '{expectedPrefix}' for {Kind} taxonomy."));
        }

        if (_terms.Any(t => t.Code == code))
        {
            return Result.Failure(new Error("E-TAXO-DUPLICATE-CODE", $"A term with code '{code.Value}' already exists in this taxonomy."));
        }

        if (Kind == TaxonomyKind.Skills)
        {
            if (category == null)
            {
                return Result.Failure(new Error("E-TAXO-CATEGORY-REQUIRED", "Skill category is required for Skills taxonomy."));
            }
        }
        else
        {
            if (category != null)
            {
                return Result.Failure(new Error("E-TAXO-INVALID-CATEGORY", "Category must be null for non-skills taxonomies."));
            }
        }

        if (parentCode != null)
        {
            var parent = _terms.FirstOrDefault(t => t.Code == parentCode);
            if (parent == null)
            {
                return Result.Failure(new Error("E-TAXO-PARENT-NOT-FOUND", $"Parent term with code '{parentCode.Value}' was not found."));
            }
            if (parent.Status == TermStatus.Deprecated)
            {
                return Result.Failure(new Error("E-TAXO-PARENT-DEPRECATED", $"Cannot set deprecated term '{parentCode.Value}' as a parent."));
            }
        }

        var term = TaxonomyTerm.Create(Guid.NewGuid(), code, label, category, parentCode);
        _terms.Add(term);

        // Only raise TermAdded event, no version bump or TaxonomyUpdated
        RaiseDomainEvent(new TaxonomyTermAddedDomainEvent(Id, Kind, code, label, category, parentCode));

        return Result.Success();
    }

    public void FinalizeImport()
    {
        if (_terms.Any())
        {
            IncrementVersion();
            RaiseDomainEvent(new TaxonomyUpdatedDomainEvent(Id, Kind, Version, $"Bulk import completed"));
        }
    }

    private void IncrementVersion()
    {
        Version++;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    private bool WouldCreateCycle(TermCode childCode, TermCode targetParentCode)
    {
        var currentParentCode = targetParentCode;
        while (currentParentCode != null)
        {
            if (currentParentCode == childCode)
            {
                return true;
            }

            var parentTerm = _terms.FirstOrDefault(t => t.Code == currentParentCode);
            currentParentCode = parentTerm?.ParentCode;
        }

        return false;
    }

    private static string GetExpectedPrefix(TaxonomyKind kind) => kind switch
    {
        TaxonomyKind.Skills => "SKILL.",
        TaxonomyKind.Occupations => "OCC.",
        TaxonomyKind.TrainingPrograms => "TRAIN.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
