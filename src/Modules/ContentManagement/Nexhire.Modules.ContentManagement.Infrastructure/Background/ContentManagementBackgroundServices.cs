using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexhire.Modules.ContentManagement.Infrastructure.Persistence;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.ContentManagement.Infrastructure.Background;

public sealed class ScheduledPublicationWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ScheduledPublicationWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    public ScheduledPublicationWorker(IServiceProvider sp, ILogger<ScheduledPublicationWorker> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ContentManagementDbContext>();
                var now = DateTime.UtcNow;

                var due = await db.Articles
                    .Where(a => a.Status == Core.Domain.Enums.ArticleStatus.Scheduled)
                    .ToListAsync(stoppingToken);

                foreach (var article in due)
                {
                    if (article.IsDueForPublication(now))
                    {
                        article.MarkPublishedBySchedule();
                    }
                }

                if (due.Any(a => a.Status == Core.Domain.Enums.ArticleStatus.Published))
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled publication worker");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}

public sealed class ContentManagementOutboxRelayBackgroundService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ContentManagementOutboxRelayBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    public ContentManagementOutboxRelayBackgroundService(
        IServiceProvider sp,
        ILogger<ContentManagementOutboxRelayBackgroundService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ContentManagementDbContext>();
                var pending = await db.OutboxMessages
                    .Where(m => m.ProcessedOnUtc == null)
                    .OrderBy(m => m.OccurredOnUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in pending)
                    msg.MarkProcessed(DateTime.UtcNow);

                if (pending.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox relay");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
