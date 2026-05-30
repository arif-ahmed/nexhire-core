using MediatR;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Contracts.Events;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.EventHandlers;

public class IdentityVerifiedByGovernmentEventHandler : INotificationHandler<IdentityVerifiedByGovernmentIntegrationEvent>
{
    private readonly IUserAccountRepository _repository;

    public IdentityVerifiedByGovernmentEventHandler(IUserAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(IdentityVerifiedByGovernmentIntegrationEvent notification, CancellationToken ct)
    {
        // Check inbox idempotency (if your context exposes Inbox)
        // For simplicity, checking if event already processed isn't strictly required if method is idempotent.
        // UserAccount.ApplyGovernmentIdentityVerified() just sets IdentityVerified = true, which is idempotent.
        
        var account = await _repository.GetByIdAsync(new UserAccountId(notification.UserId), ct);
        if (account == null) return;

        account.ApplyGovernmentIdentityVerified();
        await _repository.SaveChangesAsync(ct);
    }
}

public class IdentityVerificationFailedEventHandler : INotificationHandler<IdentityVerificationFailedIntegrationEvent>
{
    // Potentially we just log it or save to audit log, but since we don't have a status field for failed, we do nothing.
    public Task Handle(IdentityVerificationFailedIntegrationEvent notification, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
