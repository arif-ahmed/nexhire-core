using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Events;

public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

public record PartnerRegistered(Guid PartnerId, string Name, string ContactEmail) : DomainEventBase;
public record PartnerActivated(Guid PartnerId) : DomainEventBase;
public record PartnerStatusChanged(Guid PartnerId, string OldStatus, string NewStatus) : DomainEventBase;
public record ApiKeyIssued(Guid PartnerId, Guid KeyId, string KeyPrefix) : DomainEventBase;
public record ApiKeyRevoked(Guid PartnerId, Guid KeyId) : DomainEventBase;
public record GovernmentDataDeleted(Guid SeekerUserId) : DomainEventBase;
