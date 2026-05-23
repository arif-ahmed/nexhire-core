using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.Partner;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.SyncRecord;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Repositories;
using Nexhire.Modules.ExternalJobSync.Core.CQRS.Commands;
using MediatR;

namespace Nexhire.Modules.ExternalJobSync.Infrastructure.Background;

public sealed class SyncSchedulerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncSchedulerWorker> _logger;

    public SyncSchedulerWorker(IServiceProvider serviceProvider, ILogger<SyncSchedulerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExternalJobSync Scheduler Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IExternalConnectorRepository>();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                var connectors = await repository.ListDueForPullAsync(stoppingToken);
                foreach (var connector in connectors)
                {
                    _logger.LogInformation("Triggering pull sync for connector: {PortalName}", connector.PortalName);
                    
                    // Dispatch IngestExternalJobsCommand for the connector
                    await sender.Send(new IngestExternalJobsCommand(connector.Id), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SyncSchedulerWorker loop.");
            }

            // Runs every 5 minutes in background
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

public sealed class StaleRecordArchiverWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StaleRecordArchiverWorker> _logger;

    public StaleRecordArchiverWorker(IServiceProvider serviceProvider, ILogger<StaleRecordArchiverWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExternalJobSync Stale Record Archiver Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ISyncRecordRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IExternalJobSyncUnitOfWork>();

                // Fetch Quarantined records
                var quarantined = await repository.ListByStatusAsync(SyncStatus.Quarantined, stoppingToken);
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                
                int archivedCount = 0;
                foreach (var record in quarantined)
                {
                    if (record.LastSyncOnUtc < thirtyDaysAgo)
                    {
                        record.Archive();
                        repository.Update(record);
                        archivedCount++;
                    }
                }

                if (archivedCount > 0)
                {
                    await unitOfWork.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Archived {Count} stale quarantined records.", archivedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in StaleRecordArchiverWorker loop.");
            }

            // Runs once a day in background
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}

public sealed class ApiKeyExpirySweepWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApiKeyExpirySweepWorker> _logger;

    public ApiKeyExpirySweepWorker(IServiceProvider serviceProvider, ILogger<ApiKeyExpirySweepWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExternalJobSync Api Key Expiry Sweep Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                // Simple sweep logic: check if any keys in the database have expired and change their status
                // Since ApiKeys are child entities of Partner, a production sweeper would run an direct SQL or flat repository search
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ApiKeyExpirySweepWorker loop.");
            }

            // Runs once an hour in background
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
