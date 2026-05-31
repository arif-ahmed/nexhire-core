using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Events;

public record MfaEnabledEvent(Guid EventId, UserAccountId AccountId, string Method, DateTime OccurredOnUtc) : IDomainEvent;
public record MfaDisabledEvent(Guid EventId, UserAccountId AccountId, DateTime OccurredOnUtc) : IDomainEvent;
public record BackupCodeRedeemedEvent(Guid EventId, UserAccountId AccountId, DateTime OccurredOnUtc) : IDomainEvent;
public record SessionRevokedEvent(Guid EventId, UserAccountId AccountId, SessionId SessionId, DateTime OccurredOnUtc) : IDomainEvent;
public record AccountUnlockedEvent(Guid EventId, UserAccountId AccountId, DateTime OccurredOnUtc) : IDomainEvent;
public record IdentityVerificationAppliedEvent(Guid EventId, UserAccountId AccountId, DateTime OccurredOnUtc) : IDomainEvent;
public record OtpIssuedEvent(Guid EventId, UserAccountId AccountId, OtpPurpose Purpose, DateTime OccurredOnUtc) : IDomainEvent;
public record OtpVerifiedEvent(Guid EventId, UserAccountId AccountId, OtpPurpose Purpose, DateTime OccurredOnUtc) : IDomainEvent;
