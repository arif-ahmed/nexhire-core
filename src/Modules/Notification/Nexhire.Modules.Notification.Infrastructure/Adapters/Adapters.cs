using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.Notification.Domain.Ports;
using Nexhire.Modules.Notification.Domain.Repositories;
using Nexhire.Modules.Notification.Application.PublicApi;

namespace Nexhire.Modules.Notification.Infrastructure.Adapters;

public class EmailChannelStub : IEmailChannel
{
    public Task<Result<string>> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
    {
        // High fidelity stub that simulates SMTP transmission
        string providerMessageId = $"msg_email_{Guid.NewGuid():N}";
        
        Console.WriteLine($"[EmailChannelStub] Sending email to {request.ToAddress} - Subject: {request.Subject}");
        
        return Task.FromResult(Result.Success(providerMessageId));
    }
}

public class SmsChannelStub : ISmsChannel
{
    public Task<Result<string>> SendAsync(SmsSendRequest request, CancellationToken cancellationToken = default)
    {
        // High fidelity stub that simulates carrier network dispatch
        string providerMessageId = $"msg_sms_{Guid.NewGuid():N}";
        
        Console.WriteLine($"[SmsChannelStub] Sending SMS to {request.ToE164} - Body: {request.Body}");

        return Task.FromResult(Result.Success(providerMessageId));
    }
}

public class RealtimePushStub : IRealtimePush
{
    public Task<Result> PushToastAsync(Guid userId, InAppToastDto toast, CancellationToken cancellationToken = default)
    {
        // High fidelity stub that simulates WebSocket real-time push
        Console.WriteLine($"[RealtimePushStub] Pushing Toast Notification to User {userId} - Title: {toast.Title}");
        
        return Task.FromResult(Result.Success());
    }
}

public class DncRegistryStub : IDncRegistry
{
    public Task<bool> IsRegisteredAsync(string e164Number, CancellationToken cancellationToken = default)
    {
        // Simple stub: returns true for a specific mock testing number, false otherwise
        bool isBlocked = e164Number == "+8801700000000";
        return Task.FromResult(isBlocked);
    }
}

public class NotificationPublicApi : INotificationPublicApi
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationLogRepository _logRepository;

    public NotificationPublicApi(
        INotificationRepository notificationRepository,
        INotificationLogRepository logRepository)
    {
        _notificationRepository = notificationRepository;
        _logRepository = logRepository;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _notificationRepository.CountUnreadForUserAsync(userId, cancellationToken);
    }

    public async Task<List<NotificationLogEntryDto>> GetDeliveryLogAsync(
        Guid userId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await _logRepository.QueryLogsAsync(
            userId,
            fromUtc,
            toUtc,
            null,
            null,
            0,
            100,
            cancellationToken);
    }
}
