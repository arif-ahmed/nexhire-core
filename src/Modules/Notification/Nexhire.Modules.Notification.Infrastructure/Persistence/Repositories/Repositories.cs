using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.Notification.Core.Domain.Aggregates;
using Nexhire.Modules.Notification.Core.Domain.Repositories;
using Nexhire.Modules.Notification.Core.Domain;
using NotificationAggregate = Nexhire.Modules.Notification.Core.Domain.Aggregates.Notification;

namespace Nexhire.Modules.Notification.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationAggregate?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Include(n => n.Attempts)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<NotificationAggregate?> GetByProviderMessageIdAsync(string providerMessageId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Include(n => n.Attempts)
            .FirstOrDefaultAsync(n => n.ProviderMessageId == providerMessageId, cancellationToken);
    }

    public async Task<List<NotificationAggregate>> GetInAppForUserAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Include(n => n.Attempts)
            .Where(n => n.RecipientUserId == userId && n.Channel == Channel.InApp && !n.IsArchived)
            .OrderByDescending(n => n.CreatedOnUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n => n.RecipientUserId == userId && n.Channel == Channel.InApp && !n.IsRead && !n.IsArchived, cancellationToken);
    }

    public async Task<List<NotificationAggregate>> GetDueScheduledAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Include(n => n.Attempts)
            .Where(n => n.ScheduledForUtc != null && n.ScheduledForUtc <= nowUtc && (n.DeliveryStatus == DeliveryStatus.Pending || n.DeliveryStatus == DeliveryStatus.Queued))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NotificationAggregate notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    public void Update(NotificationAggregate notification)
    {
        _context.Notifications.Update(notification);
    }
}

public class RecipientPreferencesRepository : IRecipientPreferencesRepository
{
    private readonly NotificationDbContext _context;

    public RecipientPreferencesRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<RecipientPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.RecipientPreferences
            .Include(r => r.ChannelTypePrefs)
            .Include(r => r.Consents)
            .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);
    }

    public async Task<int> CountSmsSentInLast24hAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-1);
        
        // Count total SMS sent or attempts Succeeded in the last 24h
        return await _context.Notifications
            .CountAsync(n => n.RecipientUserId == userId && 
                             n.Channel == Channel.Sms && 
                             n.CreatedOnUtc >= cutoff && 
                             (n.DeliveryStatus == DeliveryStatus.Sent || n.DeliveryStatus == DeliveryStatus.Delivered),
                             cancellationToken);
    }

    public async Task AddAsync(RecipientPreferences preferences, CancellationToken cancellationToken = default)
    {
        await _context.RecipientPreferences.AddAsync(preferences, cancellationToken);
    }

    public void Update(RecipientPreferences preferences)
    {
        _context.RecipientPreferences.Update(preferences);
    }
}

public class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly NotificationDbContext _context;

    public NotificationTemplateRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationTemplate?> GetByChannelAndTypeAsync(Channel channel, NotificationType type, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Channel == channel && t.Type == type, cancellationToken);
    }

    public async Task<NotificationTemplate?> GetByIdAsync(NotificationTemplateId id, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<NotificationTemplate>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .Include(t => t.History)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        await _context.NotificationTemplates.AddAsync(template, cancellationToken);
    }

    public void Update(NotificationTemplate template)
    {
        _context.NotificationTemplates.Update(template);
    }
}

public class DigestRepository : IDigestRepository
{
    private readonly NotificationDbContext _context;

    public DigestRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<Digest?> GetOpenAsync(Guid userId, Channel channel, DigestWindow window, CancellationToken cancellationToken = default)
    {
        return await _context.Digests
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Channel == channel && d.Window == window && d.Status == DigestStatus.Open, cancellationToken);
    }

    public async Task<List<Digest>> GetDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.Digests
            .Include(d => d.Items)
            .Where(d => d.Status == DigestStatus.Open && d.ScheduledSendUtc <= nowUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Digest digest, CancellationToken cancellationToken = default)
    {
        await _context.Digests.AddAsync(digest, cancellationToken);
    }

    public void Update(Digest digest)
    {
        _context.Digests.Update(digest);
    }
}

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly NotificationDbContext _context;

    public NotificationLogRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationLogEntryDto>> QueryLogsAsync(
        Guid? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        Channel? channel,
        DeliveryStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(n => n.RecipientUserId == userId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(n => n.CreatedOnUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(n => n.CreatedOnUtc <= toUtc.Value);
        }

        if (channel.HasValue)
        {
            query = query.Where(n => n.Channel == channel.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(n => n.DeliveryStatus == status.Value);
        }

        return await query
            .OrderByDescending(n => n.CreatedOnUtc)
            .Skip(skip)
            .Take(take)
            .Select(n => new NotificationLogEntryDto(
                n.Id.Value,
                n.Channel.ToString(),
                n.Type.ToString(),
                n.DeliveryStatus.ToString(),
                n.CreatedOnUtc,
                n.DeliveryStatus == DeliveryStatus.Delivered ? n.UpdatedOnUtc : null
            ))
            .ToListAsync(cancellationToken);
    }
}

public class NotificationUnitOfWork : INotificationUnitOfWork
{
    private readonly NotificationDbContext _context;

    public NotificationUnitOfWork(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
