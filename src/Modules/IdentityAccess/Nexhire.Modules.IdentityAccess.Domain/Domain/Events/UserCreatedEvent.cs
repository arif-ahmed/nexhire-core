using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.IdentityAccess.Domain.Events;

public record UserCreatedEvent(Guid EventId, Guid UserId, DateTime OccurredOnUtc) : IDomainEvent;
