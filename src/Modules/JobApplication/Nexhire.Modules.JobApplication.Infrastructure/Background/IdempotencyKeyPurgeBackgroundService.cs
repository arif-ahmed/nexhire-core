using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexhire.Modules.JobApplication.Infrastructure.Background;

public sealed class IdempotencyKeyPurgeBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdempotencyKeyPurgeBackgroundService> _logger;

    public IdempotencyKeyPurgeBackgroundService(IServiceScopeFactory scopeFactory, ILogger<IdempotencyKeyPurgeBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run key purge every hour
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var keyStore = scope.ServiceProvider.GetRequiredService<IIdempotencyKeyStore>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IJobApplicationUnitOfWork>();

                // Purge keys older than 24 hours
                var threshold = DateTime.UtcNow.AddHours(-24);
                await keyStore.PurgeOlderThanAsync(threshold, stoppingToken);
                await unitOfWork.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Successfully completed idempotency key purge at {Time}", DateTime.UtcNow);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to purge expired idempotency keys.");
            }
        }
    }
}
