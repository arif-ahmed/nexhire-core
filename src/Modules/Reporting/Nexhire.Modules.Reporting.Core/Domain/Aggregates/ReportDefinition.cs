using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Events;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Domain.Aggregates;

public class ReportDefinitionVersion : Entity<Guid>
{
    public Guid DefinitionId { get; private set; }
    public int VersionNumber { get; private set; }
    public ReportSpec Spec { get; private set; } = null!;
    public bool IsCurrent { get; private set; }
    public Guid ChangedBy { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private ReportDefinitionVersion() { }
    internal ReportDefinitionVersion(Guid id, Guid definitionId, int versionNumber, ReportSpec spec, Guid changedBy, bool isCurrent) : base(id)
    {
        DefinitionId = definitionId; VersionNumber = versionNumber; Spec = spec;
        ChangedBy = changedBy; IsCurrent = isCurrent; CreatedOnUtc = DateTime.UtcNow;
    }
    internal void MarkNotCurrent() => IsCurrent = false;
    internal void MarkArchived() => IsCurrent = false;
}

public class ReportDefinition : AggregateRoot<Guid>
{
    private readonly List<ReportDefinitionVersion> _versions = new();

    public ReportDefinitionKind Kind { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ReportCategory Category { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public ReportSpec Spec { get; private set; } = null!;
    public List<ConfigurableParameter> ConfigurableParameters { get; private set; } = new();
    public ReportVisibility Visibility { get; private set; } = null!;
    public IReadOnlyCollection<ReportDefinitionVersion> Versions => _versions.AsReadOnly();
    public int CurrentVersionNumber { get; private set; }
    public int UsageCount { get; private set; }
    public ReportDefinitionStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private ReportDefinition() { }

    public static Result<ReportDefinition> CreateTemplate(string name, ReportCategory category, Guid ownerUserId,
        ReportSpec spec, List<ConfigurableParameter> configurableParameters, ReportVisibility visibility)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            return Result.Failure<ReportDefinition>(new Error("ReportDefinition.InvalidName", "Name must be non-empty and ≤200 chars."));

        var def = new ReportDefinition
        {
            Id = Guid.NewGuid(), Kind = ReportDefinitionKind.Template, Name = name.Trim(),
            Category = category, OwnerUserId = ownerUserId, Spec = spec,
            ConfigurableParameters = configurableParameters, Visibility = visibility,
            CurrentVersionNumber = 1, Status = ReportDefinitionStatus.Active,
            CreatedOnUtc = DateTime.UtcNow, UpdatedOnUtc = DateTime.UtcNow
        };
        def._versions.Add(new ReportDefinitionVersion(Guid.NewGuid(), def.Id, 1, spec, ownerUserId, true));
        def.RaiseDomainEvent(new ReportDefinitionCreated(Guid.NewGuid(), def.Id, def.CreatedOnUtc));
        return Result.Success(def);
    }

    public static Result<ReportDefinition> CreateCustom(string name, Guid ownerUserId, ReportSpec spec)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            return Result.Failure<ReportDefinition>(new Error("ReportDefinition.InvalidName", "Name must be non-empty and ≤200 chars."));

        var visibility = ReportVisibility.Create(new HashSet<string> { "SystemAdministrator" }).Value;
        var def = new ReportDefinition
        {
            Id = Guid.NewGuid(), Kind = ReportDefinitionKind.Custom, Name = name.Trim(),
            Category = ReportCategory.Custom, OwnerUserId = ownerUserId, Spec = spec,
            ConfigurableParameters = new List<ConfigurableParameter>(), Visibility = visibility,
            CurrentVersionNumber = 1, Status = ReportDefinitionStatus.Active,
            CreatedOnUtc = DateTime.UtcNow, UpdatedOnUtc = DateTime.UtcNow
        };
        def._versions.Add(new ReportDefinitionVersion(Guid.NewGuid(), def.Id, 1, spec, ownerUserId, true));
        def.RaiseDomainEvent(new ReportDefinitionCreated(Guid.NewGuid(), def.Id, def.CreatedOnUtc));
        return Result.Success(def);
    }

    public Result SaveCustomAsTemplate(ReportCategory category, List<ConfigurableParameter> configurableParameters, ReportVisibility visibility)
    {
        if (Kind != ReportDefinitionKind.Custom)
            return Result.Failure(new Error("E-REPORT-NOT-CUSTOM", "Only custom reports can be saved as templates."));
        Kind = ReportDefinitionKind.Template;
        Category = category;
        ConfigurableParameters = configurableParameters;
        Visibility = visibility;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ReportDefinitionCreated(Guid.NewGuid(), Id, UpdatedOnUtc));
        return Result.Success();
    }

    public Result UpdateSpec(ReportSpec newSpec, Guid changedBy)
    {
        foreach (var v in _versions.Where(v => v.IsCurrent)) v.MarkNotCurrent();
        CurrentVersionNumber++;
        _versions.Add(new ReportDefinitionVersion(Guid.NewGuid(), Id, CurrentVersionNumber, newSpec, changedBy, true));
        Spec = newSpec;

        var archiveCutoff = CurrentVersionNumber - 5;
        foreach (var v in _versions.Where(v => v.VersionNumber <= archiveCutoff && !v.IsCurrent))
            v.MarkArchived();

        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ReportDefinitionVersioned(Guid.NewGuid(), Id, CurrentVersionNumber, UpdatedOnUtc));
        return Result.Success();
    }

    public Result UpdateVisibility(ReportVisibility newVisibility)
    {
        Visibility = newVisibility;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void RecordUsage() => UsageCount++;

    public Result Archive()
    {
        if (Status != ReportDefinitionStatus.Active)
            return Result.Failure(new Error("ReportDefinition.NotActive", "Only active definitions can be archived."));
        Status = ReportDefinitionStatus.Archived;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }
}
