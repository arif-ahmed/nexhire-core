using MediatR;
using Microsoft.Extensions.Logging;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Contracts.Events;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.EventHandlers;

public sealed class IdentityVerifiedByGovernmentEventHandler(
    IUserAccountRepository repository,
    IInboxStore inbox)
    : INotificationHandler<IdentityVerifiedByGovernmentIntegrationEvent>
{
    public async Task Handle(
        IdentityVerifiedByGovernmentIntegrationEvent notification,
        CancellationToken ct)
    {
        // Inbox idempotency: skip if this event was already processed (Shared Foundations §6.3)
        if (await inbox.IsProcessedAsync(notification.EventId, ct))
            return;

        var account = await repository.GetByIdAsync(new UserAccountId(notification.UserId), ct);
        account?.ApplyGovernmentIdentityVerified();

        // Mark processed and persist in the same unit of work as the aggregate change
        await inbox.MarkProcessedAsync(
            notification.EventId,
            nameof(IdentityVerifiedByGovernmentIntegrationEvent),
            ct);

        await repository.SaveChangesAsync(ct);
    }
}

public sealed class IdentityVerificationFailedEventHandler(
    IUserAccountRepository repository,
    IInboxStore inbox,
    ILogger<IdentityVerificationFailedEventHandler> logger)
    : INotificationHandler<IdentityVerificationFailedIntegrationEvent>
{
    public async Task Handle(
        IdentityVerificationFailedIntegrationEvent notification,
        CancellationToken ct)
    {
        // Inbox idempotency
        if (await inbox.IsProcessedAsync(notification.EventId, ct))
            return;

        // Spec §9.1: record failure for audit — no account status change
        logger.LogWarning(
            "Government identity verification failed for user {UserId} via registry {Registry}. Reason: {Reason}",
            notification.UserId,
            notification.Registry,
            notification.Reason);

        await inbox.MarkProcessedAsync(
            notification.EventId,
            nameof(IdentityVerificationFailedIntegrationEvent),
            ct);

        // Persist the inbox record (repository shares the same DbContext scope as InboxStore)
        await repository.SaveChangesAsync(ct);
    }
}
