using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.JobPostings.Core.Domain.Events;

public abstract record JobPostingEvent(DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

public sealed record JobPostingCreatedIntegrationEvent(Guid JobPostingId, Guid EmployerId, DateTime OccurredOnUtc) : JobPostingEvent(OccurredOnUtc);
public sealed record JobPostingPublishedIntegrationEvent(Guid JobPostingId, Guid EmployerId, string Title, string ContractType, string WorkFormat, IReadOnlyCollection<string> RequiredSkillCodes, DateTime DeadlineUtc, string Visibility, DateTime OccurredOnUtc) : JobPostingEvent(OccurredOnUtc);
public sealed record JobPostingUpdatedIntegrationEvent(Guid JobPostingId, IReadOnlyCollection<string> ChangedFields, IReadOnlyCollection<string> RequiredSkillCodes, DateTime OccurredOnUtc) : JobPostingEvent(OccurredOnUtc);
public sealed record JobPostingExpiredIntegrationEvent(Guid JobPostingId, Guid EmployerId, DateTime ExpiredOnUtc) : JobPostingEvent(ExpiredOnUtc);
public sealed record JobPostingClosedIntegrationEvent(Guid JobPostingId, Guid EmployerId, string Reason, DateTime OccurredOnUtc) : JobPostingEvent(OccurredOnUtc);
public sealed record JobPostingSuspendedIntegrationEvent(Guid JobPostingId, Guid EmployerId, string Reason, DateTime OccurredOnUtc) : JobPostingEvent(OccurredOnUtc);
public sealed record JobPostingReinstatedIntegrationEvent(Guid JobPostingId, Guid EmployerId, DateTime OccurredOnUtc) : JobPostingEvent(OccurredOnUtc);
public sealed record JobPostingStatusChangedIntegrationEvent(Guid JobPostingId, string FromStatus, string ToStatus, bool IsSearchable, bool IsAcceptingApplications, string ActorKind, DateTime OccurredOnUtc) : JobPostingEvent(OccurredOnUtc);
public sealed record JobPostingRenewedEvent(Guid NewJobPostingId, Guid RenewedFromPostingId, DateTime OccurredOnUtc) : JobPostingEvent(OccurredOnUtc);
