using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexhire.Modules.JobPostings.Infrastructure.Persistence;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.JobPostings.Infrastructure.Background;

public sealed class JobPostingsOutboxRelayBackgroundService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobPostingsOutboxRelayBackgroundService> _logger;

    public JobPostingsOutboxRelayBackgroundService(IServiceScopeFactory scopeFactory, ILogger<JobPostingsOutboxRelayBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RelayBatch(stoppingToken);
        }
    }

    private async Task RelayBatch(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<JobPostingsDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            var messages = await dbContext.OutboxMessages
                .Where(x => x.ProcessedOnUtc == null)
                .OrderBy(x => x.OccurredOnUtc)
                .Take(20)
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                var type = Type.GetType(message.Type);
                if (type is null || !typeof(IDomainEvent).IsAssignableFrom(type))
                {
                    message.MarkFailed($"Unable to resolve outbox message type '{message.Type}'.");
                    continue;
                }

                var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(message.Content, type, JsonOptions);
                if (domainEvent is null)
                {
                    message.MarkFailed("Unable to deserialize outbox message content.");
                    continue;
                }

                await publisher.Publish(domainEvent, cancellationToken);
                message.MarkProcessed(DateTime.UtcNow);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to relay JobPostings outbox messages.");
        }
    }
}
