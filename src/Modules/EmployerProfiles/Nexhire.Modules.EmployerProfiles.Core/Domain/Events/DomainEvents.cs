using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Events;

public record EmployerProfileActivated(Guid EventId, Guid EmployerProfileId, DateTime OccurredOnUtc) : IDomainEvent;

public record SupplementaryDocumentAdded(Guid EventId, Guid EmployerProfileId, Guid DocumentId, DateTime OccurredOnUtc) : IDomainEvent;

public record SupplementaryDocumentRemoved(Guid EventId, Guid EmployerProfileId, Guid DocumentId, DateTime OccurredOnUtc) : IDomainEvent;

public record EmployerProfileSuspended(Guid EventId, Guid EmployerProfileId, string Reason, DateTime OccurredOnUtc) : IDomainEvent;

public record EmployerProfileReinstated(Guid EventId, Guid EmployerProfileId, DateTime OccurredOnUtc) : IDomainEvent;

public record EmployerProfileDeactivated(Guid EventId, Guid EmployerProfileId, DateTime OccurredOnUtc) : IDomainEvent;

public record CandidateRemovedFromTalentPool(Guid EventId, Guid ShortlistId, Guid CandidateUserId, DateTime OccurredOnUtc) : IDomainEvent;

public record ShortlistDeleted(Guid EventId, Guid ShortlistId, DateTime OccurredOnUtc) : IDomainEvent;
