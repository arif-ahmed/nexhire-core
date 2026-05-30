using System.Collections.Concurrent;
using Nexhire.Modules.IdentityAccess.Application.Ports;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.PortAdapters;

public class RateLimiterPortStub : IRateLimiterPort
{
    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _limits = new();

    public Task<bool> TryConsumeAsync(string key, int maxInWindow, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        _limits.AddOrUpdate(key, 
            _ => (1, now),
            (_, current) => 
            {
                if (now - current.WindowStart > window)
                {
                    return (1, now);
                }
                return (current.Count + 1, current.WindowStart);
            });

        if (_limits.TryGetValue(key, out var state))
        {
            if (state.Count > maxInWindow)
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
}
