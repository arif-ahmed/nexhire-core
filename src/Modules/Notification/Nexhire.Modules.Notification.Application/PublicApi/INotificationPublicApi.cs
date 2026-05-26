using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.Notification.Domain.Repositories;

namespace Nexhire.Modules.Notification.Application.PublicApi;

public interface INotificationPublicApi
{
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task<List<NotificationLogEntryDto>> GetDeliveryLogAsync(
        Guid userId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
