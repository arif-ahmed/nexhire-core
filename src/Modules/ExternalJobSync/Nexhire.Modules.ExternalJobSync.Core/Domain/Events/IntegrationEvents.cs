using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Events;

public abstract record IntegrationEventBase : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

public record ExternalJobIngestedIntegrationEvent(
    ExternalRef ExternalRef,
    Guid? PartnerId,
    NormalisedJobPosting NormalisedPosting,
    DateTime OccurredOnUtc) : IntegrationEventBase;

public record ExternalJobUpdatedIntegrationEvent(
    ExternalRef ExternalRef,
    Guid? PartnerId,
    List<string> ChangedFields,
    NormalisedJobPosting NormalisedPosting,
    DateTime OccurredOnUtc) : IntegrationEventBase;

public record ExternalJobRetractedIntegrationEvent(
    ExternalRef ExternalRef,
    Guid? PartnerId,
    DateTime OccurredOnUtc) : IntegrationEventBase;

public record IdentityVerifiedByGovernmentIntegrationEvent(
    Guid UserId,
    string Registry,
    DateTime VerifiedOnUtc,
    DateTime OccurredOnUtc) : IntegrationEventBase;

public record IdentityVerificationFailedIntegrationEvent(
    Guid UserId,
    string Registry,
    string Reason,
    DateTime OccurredOnUtc) : IntegrationEventBase;

public record EducationVerifiedIntegrationEvent(
    Guid JobSeekerProfileId,
    string CredentialRef,
    DateTime VerifiedOnUtc,
    DateTime OccurredOnUtc) : IntegrationEventBase;

public record EmployerVerifiedByGovernmentIntegrationEvent(
    Guid EmployerId,
    string Registry,
    DateTime VerifiedOnUtc,
    DateTime OccurredOnUtc) : IntegrationEventBase;

public record SyncErrorDetectedIntegrationEvent(
    Guid? PartnerId,
    Guid? ConnectorId,
    string ErrorClass,
    Guid PayloadRef,
    DateTime OccurredOnUtc) : IntegrationEventBase;

public record SyncReconciledIntegrationEvent(
    Guid? PartnerId,
    Guid? ConnectorId,
    int RecordsAffected,
    DateTime OccurredOnUtc) : IntegrationEventBase;
