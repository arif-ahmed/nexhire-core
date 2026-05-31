namespace Nexhire.Modules.IdentityAccess.Application.Ports;

/// <summary>
/// Inbox idempotency store — prevents duplicate processing of inbound integration events.
/// Checked and written in the same transaction as the domain change (Shared Foundations §6.3).
/// </summary>
public interface IInboxStore
{
    Task<bool> IsProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
