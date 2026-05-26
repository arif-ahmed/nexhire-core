using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.Notification.Application.CQRS.Commands;
using Nexhire.Modules.Notification.Domain.Repositories;
using Nexhire.Modules.Notification.Domain.Aggregates;
using Nexhire.Modules.Notification.Domain;
using Nexhire.Modules.Notification.Infrastructure.Persistence;

namespace Nexhire.Modules.Notification.Infrastructure.Background;

public class OutboxRelayWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxRelayWorker> _logger;

    public OutboxRelayWorker(IServiceProvider serviceProvider, ILogger<OutboxRelayWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Persistence.NotificationDbContext>();
                
                // Sweep outbox messages that are unprocessed
                var unsent = dbContext.OutboxMessages
                    .Where(o => o.ProcessedOnUtc == null)
                    .Take(20)
                    .ToList();

                foreach (var msg in unsent)
                {
                    _logger.LogInformation($"[OutboxRelayWorker] Processing outbox message {msg.Id} of type {msg.Type}");
                    
                    msg.MarkProcessed(DateTime.UtcNow);
                    dbContext.OutboxMessages.Update(msg);
                }

                if (unsent.Any())
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OutboxRelayWorker] Error running outbox relay sweep.");
            }
        }
    }
}

public class DigestSchedulerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DigestSchedulerWorker> _logger;

    public DigestSchedulerWorker(IServiceProvider serviceProvider, ILogger<DigestSchedulerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var digestRepository = scope.ServiceProvider.GetRequiredService<IDigestRepository>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var now = DateTime.UtcNow;
                var dueDigests = await digestRepository.GetDueAsync(now, stoppingToken);

                foreach (var digest in dueDigests)
                {
                    _logger.LogInformation($"[DigestSchedulerWorker] Dispatching due digest {digest.Id} for user {digest.UserId}");
                    await mediator.Send(new DispatchDigestCommand(digest.Id.Value), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DigestSchedulerWorker] Error running digest scheduler sweep.");
            }
        }
    }
}

public class DndReleaseWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DndReleaseWorker> _logger;

    public DndReleaseWorker(IServiceProvider serviceProvider, ILogger<DndReleaseWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var now = DateTime.UtcNow;
                var dueNotifications = await notificationRepository.GetDueScheduledAsync(now, stoppingToken);

                foreach (var notif in dueNotifications)
                {
                    _logger.LogInformation($"[DndReleaseWorker] Releasing held DND notification {notif.Id} for user {notif.RecipientUserId}");
                    await mediator.Send(new SendImmediateNotificationCommand(notif.Id), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DndReleaseWorker] Error running DND release sweep.");
            }
        }
    }
}

public class SoftBounceRetryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SoftBounceRetryWorker> _logger;

    public SoftBounceRetryWorker(IServiceProvider serviceProvider, ILogger<SoftBounceRetryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Persistence.NotificationDbContext>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // Fetch Sent notifications that have a SoftBounce and attempts < 3
                var softBounced = dbContext.Notifications
                    .Include(n => n.Attempts)
                    .Where(n => n.DeliveryStatus == DeliveryStatus.Sent)
                    .ToList()
                    .Where(n => n.Attempts.Any() && n.Attempts.Last().Outcome == AttemptOutcome.SoftBounce && n.Attempts.Count < 3)
                    .ToList();

                foreach (var notif in softBounced)
                {
                    var lastAttempt = notif.Attempts.Last();
                    var age = DateTime.UtcNow.Subtract(lastAttempt.AttemptedOnUtc);
                    
                    // Schedule: 1h, 6h, 24h
                    int attemptNo = notif.Attempts.Count;
                    bool shouldRetry = (attemptNo == 1 && age.TotalHours >= 1) ||
                                       (attemptNo == 2 && age.TotalHours >= 6);

                    if (shouldRetry)
                    {
                        _logger.LogInformation($"[SoftBounceRetryWorker] Retrying notification {notif.Id} (Attempt {attemptNo + 1})");
                        await mediator.Send(new SendImmediateNotificationCommand(notif.Id), stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SoftBounceRetryWorker] Error running soft bounce retry sweep.");
            }
        }
    }
}

public class RetentionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetentionWorker> _logger;

    public RetentionWorker(IServiceProvider serviceProvider, ILogger<RetentionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Nightly sweep: runs once per 24 hours (simulated or periodic timer)
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Persistence.NotificationDbContext>();

                var cutoff90Days = DateTime.UtcNow.AddDays(-90);
                var cutoff3Years = DateTime.UtcNow.AddYears(-3);
                var cutoff12Months = DateTime.UtcNow.AddDays(-365);

                _logger.LogInformation("[RetentionWorker] Running nightly clean-up and archival sweeps.");

                // 1. Soft-archive InApp notifications older than 90 days
                var oldInApp = dbContext.Notifications
                    .Where(n => n.Channel == Channel.InApp && n.CreatedOnUtc < cutoff90Days && !n.IsArchived)
                    .ToList();

                foreach (var notif in oldInApp)
                {
                    notif.Archive();
                    dbContext.Notifications.Update(notif);
                }

                // 2. Purge old template version history older than 12 months
                var templates = dbContext.NotificationTemplates.Include(t => t.History).ToList();
                foreach (var template in templates)
                {
                    template.PurgeHistoryOlderThan(cutoff12Months);
                    dbContext.NotificationTemplates.Update(template);
                }

                // 3. Purge expired digest items older than 30 days
                var openDigests = dbContext.Digests.Include(d => d.Items).Where(d => d.Status == DigestStatus.Open).ToList();
                foreach (var digest in openDigests)
                {
                    digest.RemoveExpiredItems(DateTime.UtcNow.AddDays(-30));
                    dbContext.Digests.Update(digest);
                }

                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RetentionWorker] Error running data retention worker.");
            }
        }
    }
}
