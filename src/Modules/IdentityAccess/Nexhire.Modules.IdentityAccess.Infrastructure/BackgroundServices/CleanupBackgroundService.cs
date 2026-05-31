using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexhire.Modules.IdentityAccess.Infrastructure.Persistence;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.BackgroundServices;

public class CleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public CleanupBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();

            var threshold = DateTime.UtcNow.AddDays(-7);

            var oldTokens = await dbContext.RevokedTokens
                .Where(rt => rt.ExpiresOnUtc < threshold)
                .ToListAsync(stoppingToken);
            dbContext.RevokedTokens.RemoveRange(oldTokens);

            var oldChallenges = await dbContext.OtpChallenges
                .Where(oc => oc.ExpiresOnUtc < threshold)
                .ToListAsync(stoppingToken);
            dbContext.OtpChallenges.RemoveRange(oldChallenges);

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
