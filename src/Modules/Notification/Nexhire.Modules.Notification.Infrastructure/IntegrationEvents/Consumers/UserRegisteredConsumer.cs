using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexhire.Modules.IdentityAccess.Contracts.Events;
using Nexhire.Modules.Notification.Infrastructure.Persistence;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.Notification.Infrastructure.IntegrationEvents.Consumers;

public sealed class UserRegisteredConsumer : INotificationHandler<UserRegisteredIntegrationEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(NotificationDbContext dbContext, ILogger<UserRegisteredConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        _logger.LogInformation(
            "[UserRegisteredConsumer] Received UserRegistered event for UserId={UserId}, Role={Role}, Email={Email} — would send welcome notification",
            notification.UserId, notification.Role, notification.Email);

        _dbContext.InboxMessages.Add(new InboxMessage(
            notification.EventId,
            nameof(UserRegisteredIntegrationEvent),
            DateTime.UtcNow));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
