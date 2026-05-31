using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Infrastructure.Persistence;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.BackgroundServices;

public class OtpExpirySweepBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public OtpExpirySweepBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();

            var challenges = await dbContext.OtpChallenges
                .Where(o => o.Status == OtpStatus.Issued && o.ExpiresOnUtc <= DateTime.UtcNow)
                .ToListAsync(stoppingToken);

            foreach (var c in challenges)
            {
                c.MarkExpired();
            }

            if (challenges.Any())
            {
                await dbContext.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
