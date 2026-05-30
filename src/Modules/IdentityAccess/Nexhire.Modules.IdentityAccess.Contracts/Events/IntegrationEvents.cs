using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.IdentityAccess.Contracts.Events;

public record UserRegisteredIntegrationEvent(
    Guid EventId, Guid UserId, string Role, string Email, DateTime CreatedAt, DateTime OccurredOnUtc) : IDomainEvent;

public record UserAccountActivatedIntegrationEvent(
    Guid EventId, Guid UserId, DateTime ActivatedAt, DateTime OccurredOnUtc) : IDomainEvent;

public record UserAccountSuspendedIntegrationEvent(
    Guid EventId, Guid UserId, string Reason, Guid By, DateTime At, DateTime OccurredOnUtc) : IDomainEvent;

public record UserAccountReinstatedIntegrationEvent(
    Guid EventId, Guid UserId, Guid? By, DateTime At, DateTime OccurredOnUtc) : IDomainEvent;

public record AccountDeactivatedIntegrationEvent(
    Guid EventId, Guid UserId, DateTime DeactivatedAt, DateTime OccurredOnUtc) : IDomainEvent;

public record UserLoggedInIntegrationEvent(
    Guid EventId, Guid UserId, Guid SessionId, string Channel, DateTime At, DateTime OccurredOnUtc) : IDomainEvent;

public record UserLoginFailedIntegrationEvent(
    Guid EventId, string Identifier, string Reason, DateTime At, DateTime OccurredOnUtc) : IDomainEvent;

public record PasswordResetIntegrationEvent(
    Guid EventId, Guid UserId, DateTime At, DateTime OccurredOnUtc) : IDomainEvent;

public record RoleAssignedIntegrationEvent(
    Guid EventId, Guid UserId, string Role, Guid By, DateTime At, DateTime OccurredOnUtc) : IDomainEvent;

public record IdentityVerifiedByGovernmentIntegrationEvent(
    Guid EventId, Guid UserId, string Registry, DateTime At, DateTime OccurredOnUtc) : IDomainEvent;

public record IdentityVerificationFailedIntegrationEvent(
    Guid EventId, Guid UserId, string Registry, string Reason, DateTime At, DateTime OccurredOnUtc) : IDomainEvent;
