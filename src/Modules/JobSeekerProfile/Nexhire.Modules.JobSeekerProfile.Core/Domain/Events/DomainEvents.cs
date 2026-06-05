using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Events;

public record ProfileActivatedEvent(Guid EventId, Guid ProfileId, DateTime OccurredOnUtc) : IDomainEvent;

public record ProfileDeactivatedEvent(Guid EventId, Guid ProfileId, DateTime OccurredOnUtc) : IDomainEvent;

public record ProfileReactivatedEvent(Guid EventId, Guid ProfileId, DateTime OccurredOnUtc) : IDomainEvent;

public record ProfileVerificationChangedEvent(Guid EventId, Guid ProfileId, VerificationFlags VerificationFlags, DateTime OccurredOnUtc) : IDomainEvent;

public record ResumeScanFailedEvent(Guid EventId, Guid ProfileId, Guid ResumeId, string Reason, DateTime OccurredOnUtc) : IDomainEvent;

public record ResumeParseFailedEvent(Guid EventId, Guid ProfileId, Guid ResumeId, string Reason, DateTime OccurredOnUtc) : IDomainEvent;

public record ResumeFieldsConfirmedEvent(Guid EventId, Guid ProfileId, Guid ResumeId, IReadOnlyCollection<string> ConfirmedFields, DateTime OccurredOnUtc) : IDomainEvent;

public record PublicSharingEnabledEvent(Guid EventId, Guid ProfileId, string Slug, DateTime OccurredOnUtc) : IDomainEvent;

public record PublicSharingDisabledEvent(Guid EventId, Guid ProfileId, DateTime OccurredOnUtc) : IDomainEvent;

public record PublicSharingSlugRegeneratedEvent(Guid EventId, Guid ProfileId, string NewSlug, DateTime OccurredOnUtc) : IDomainEvent;

public record ProfileRestoredEvent(Guid EventId, Guid ProfileId, Guid VersionId, DateTime OccurredOnUtc) : IDomainEvent;
