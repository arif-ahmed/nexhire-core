using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Events;

public abstract record TaxonomyEvent(DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

public sealed record TaxonomyCreatedDomainEvent(
    Guid TaxonomyId,
    TaxonomyKind Kind,
    DateTime OccurredOnUtc) : TaxonomyEvent(OccurredOnUtc);

public sealed record TaxonomyTermAddedDomainEvent(
    Guid TaxonomyId,
    TaxonomyKind Kind,
    TermCode Code,
    string Label,
    SkillCategory? Category,
    TermCode? ParentCode,
    DateTime OccurredOnUtc) : TaxonomyEvent(OccurredOnUtc)
{
    public TaxonomyTermAddedDomainEvent(
        Guid taxonomyId,
        TaxonomyKind kind,
        TermCode code,
        string label,
        SkillCategory? category,
        TermCode? parentCode)
        : this(taxonomyId, kind, code, label, category, parentCode, DateTime.UtcNow)
    {
    }
}

public sealed record TaxonomyTermDeprecatedDomainEvent(
    Guid TaxonomyId,
    TaxonomyKind Kind,
    TermCode Code,
    TermCode? ReplacedByCode,
    DateTime OccurredOnUtc) : TaxonomyEvent(OccurredOnUtc)
{
    public TaxonomyTermDeprecatedDomainEvent(
        Guid taxonomyId,
        TaxonomyKind kind,
        TermCode code,
        TermCode? replacedByCode)
        : this(taxonomyId, kind, code, replacedByCode, DateTime.UtcNow)
    {
    }
}

public sealed record TaxonomyUpdatedDomainEvent(
    Guid TaxonomyId,
    TaxonomyKind Kind,
    int Version,
    string ChangeSummary,
    DateTime OccurredOnUtc) : TaxonomyEvent(OccurredOnUtc)
{
    public TaxonomyUpdatedDomainEvent(
        Guid taxonomyId,
        TaxonomyKind kind,
        int version,
        string changeSummary)
        : this(taxonomyId, kind, version, changeSummary, DateTime.UtcNow)
    {
    }
}
