using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexhire.Modules.IdentityAccess.Infrastructure.Persistence;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.BackgroundServices;

public class SessionExpirySweepBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public SessionExpirySweepBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();

            var usersWithSessions = await dbContext.UserAccounts
                .Include(u => u.Sessions)
                .Where(u => u.Sessions.Any(s => !s.IsRevoked && s.ExpiresOnUtc <= DateTime.UtcNow))
                .ToListAsync(stoppingToken);

            bool anyChanges = false;
            foreach (var user in usersWithSessions)
            {
                var expiredSessions = user.Sessions.Where(s => !s.IsRevoked && s.ExpiresOnUtc <= DateTime.UtcNow).ToList();
                foreach (var session in expiredSessions)
                {
                    session.Revoke(DateTime.UtcNow); // Revoke implicitly expires/invalidates it
                    anyChanges = true;
                }
            }

            if (anyChanges)
            {
                await dbContext.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
