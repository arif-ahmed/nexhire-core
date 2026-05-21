using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Events;

public record EmployerRegisteredIntegrationEvent(
    Guid EventId,
    Guid EmployerProfileId,
    Guid UserId,
    string CompanyName,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

public record EmployerProfileUpdatedIntegrationEvent(
    Guid EventId,
    Guid EmployerProfileId,
    List<string> ChangedFields,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

public record EmployerVerificationRequestedIntegrationEvent(
    Guid EventId,
    Guid EmployerProfileId,
    string RegistryRef,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

public record EmployerVerifiedIntegrationEvent(
    Guid EventId,
    Guid EmployerProfileId,
    DateTime VerifiedAt,
    string EvidenceRef,
    DateTime OccurredOnUtc) : IDomainEvent;

public record EmployerVerificationFailedIntegrationEvent(
    Guid EventId,
    Guid EmployerProfileId,
    string Reason,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

public record EmployerManualVerificationRequiredIntegrationEvent(
    Guid EventId,
    Guid EmployerProfileId,
    string Reason, // "auto-verification-failed" or "resubmitted"
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

public record CandidateSavedToTalentPoolIntegrationEvent(
    Guid EventId,
    Guid EmployerId, // BC-1 identity (UserId)
    Guid JobSeekerId, // Candidate UserId
    Guid PoolId, // ShortlistId
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;
