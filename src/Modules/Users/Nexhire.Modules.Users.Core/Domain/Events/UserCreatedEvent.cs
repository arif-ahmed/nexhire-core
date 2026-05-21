using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.Users.Core.Domain.Events;

public record UserCreatedEvent(Guid EventId, Guid UserId, DateTime OccurredOnUtc) : IDomainEvent;
