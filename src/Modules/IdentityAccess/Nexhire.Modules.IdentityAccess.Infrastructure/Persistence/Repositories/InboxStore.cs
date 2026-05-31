using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Repositories;

public sealed class InboxStore : IInboxStore
{
    private readonly IdentityAccessDbContext _dbContext;

    public InboxStore(IdentityAccessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken ct = default)
        => await _dbContext.InboxMessages.AnyAsync(m => m.Id == eventId, ct);

    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default)
    {
        var message = new InboxMessage(eventId, eventType, DateTime.UtcNow);
        await _dbContext.InboxMessages.AddAsync(message, ct);
    }
}
