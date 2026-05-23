using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NotificationAggregate = Nexhire.Modules.Notification.Core.Domain.Aggregates.Notification;
using Nexhire.Modules.Notification.Core.Domain.Aggregates;

namespace Nexhire.Modules.Notification.Core.Domain.Repositories;

public interface INotificationRepository
{
    Task<NotificationAggregate?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default);
    Task<NotificationAggregate?> GetByProviderMessageIdAsync(string providerMessageId, CancellationToken cancellationToken = default);
    Task<List<NotificationAggregate>> GetInAppForUserAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<NotificationAggregate>> GetDueScheduledAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationAggregate notification, CancellationToken cancellationToken = default);
    void Update(NotificationAggregate notification);
}

public interface IRecipientPreferencesRepository
{
    Task<RecipientPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountSmsSentInLast24hAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(RecipientPreferences preferences, CancellationToken cancellationToken = default);
    void Update(RecipientPreferences preferences);
}

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetByChannelAndTypeAsync(Channel channel, NotificationType type, CancellationToken cancellationToken = default);
    Task<NotificationTemplate?> GetByIdAsync(NotificationTemplateId id, CancellationToken cancellationToken = default);
    Task<List<NotificationTemplate>> ListAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken = default);
    void Update(NotificationTemplate template);
}

public interface IDigestRepository
{
    Task<Digest?> GetOpenAsync(Guid userId, Channel channel, DigestWindow window, CancellationToken cancellationToken = default);
    Task<List<Digest>> GetDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    Task AddAsync(Digest digest, CancellationToken cancellationToken = default);
    void Update(Digest digest);
}

public interface INotificationLogRepository
{
    Task<List<NotificationLogEntryDto>> QueryLogsAsync(
        Guid? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        Channel? channel,
        DeliveryStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}

public interface INotificationUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public record NotificationLogEntryDto(
    Guid NotificationId,
    string Channel,
    string Type,
    string DeliveryStatus,
    DateTime CreatedOnUtc,
    DateTime? DeliveredOnUtc);
