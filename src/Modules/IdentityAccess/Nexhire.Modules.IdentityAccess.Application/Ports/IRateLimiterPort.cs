namespace Nexhire.Modules.IdentityAccess.Application.Ports;

public interface IRateLimiterPort
{
    Task<bool> TryConsumeAsync(string key, int maxInWindow, TimeSpan window, CancellationToken cancellationToken = default);
}
