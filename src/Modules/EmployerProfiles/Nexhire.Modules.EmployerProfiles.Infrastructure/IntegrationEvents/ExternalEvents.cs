using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.IntegrationEvents;

public record UserAccountActivatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid UserId) : IDomainEvent;

public record AccountDeactivatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid UserId) : IDomainEvent;

public record UserAccountSuspendedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid UserId,
    string Reason) : IDomainEvent;

public record UserAccountReinstatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid UserId) : IDomainEvent;

public record EmployerVerifiedByGovernmentIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid EmployerProfileId,
    string EvidenceRef) : IDomainEvent;

public record EmployerVerificationFailedByGovernmentIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid EmployerProfileId) : IDomainEvent;

public record JobPostingPublishedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid PostingId,
    Guid EmployerUserId,
    string Title) : IDomainEvent;

public record JobPostingClosedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid PostingId) : IDomainEvent;

public record ApplicationSubmittedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ApplicationId,
    Guid EmployerUserId,
    Guid PostingId,
    Guid JobSeekerId) : IDomainEvent;

public record CandidateRecommendationGeneratedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid RecommendationId,
    Guid EmployerUserId,
    Guid PostingId,
    Guid CandidateUserId,
    int MatchScore) : IDomainEvent;
