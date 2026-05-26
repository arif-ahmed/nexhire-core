using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.Notification.Domain.Events;

namespace Nexhire.Modules.Notification.Domain.Aggregates;

public sealed class DigestItem : Entity<Guid>
{
    public NotificationId NotificationId { get; private set; } = null!;
    public NotificationType Type { get; private set; }
    public string Summary { get; private set; } = null!;
    public string? ActionUrl { get; private set; }
    public DateTime QueuedOnUtc { get; private set; }

    private DigestItem() { } // EF Core

    internal DigestItem(
        Guid id,
        NotificationId notificationId,
        NotificationType type,
        string summary,
        string? actionUrl,
        DateTime queuedOnUtc) : base(id)
    {
        NotificationId = notificationId;
        Type = type;
        Summary = summary;
        ActionUrl = actionUrl;
        QueuedOnUtc = queuedOnUtc;
    }
}

public sealed class Digest : AggregateRoot<DigestId>
{
    private readonly List<DigestItem> _items = new();

    public Guid UserId { get; private set; }
    public Channel Channel { get; private set; }
    public DigestWindow Window { get; private set; }
    public DigestStatus Status { get; private set; }
    public DateTime OpenedOnUtc { get; private set; }
    public DateTime ScheduledSendUtc { get; private set; }
    public DateTime? DispatchedOnUtc { get; private set; }

    public IReadOnlyCollection<DigestItem> Items => _items.AsReadOnly();

    private Digest() { } // EF Core

    private Digest(
        DigestId id,
        Guid userId,
        Channel channel,
        DigestWindow window,
        DateTime scheduledSendUtc,
        DateTime nowUtc) : base(id)
    {
        UserId = userId;
        Channel = channel;
        Window = window;
        Status = DigestStatus.Open;
        OpenedOnUtc = nowUtc;
        ScheduledSendUtc = scheduledSendUtc;
    }

    public static Result<Digest> Open(
        Guid userId,
        Channel channel,
        DigestWindow window,
        DateTime scheduledSendUtc,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
            return Result.Failure<Digest>(new Error("Digest.UserRequired", "User ID is required."));
        if (channel == Channel.Sms)
            return Result.Failure<Digest>(new Error("Digest.SmsNotSupported", "SMS channel cannot be batched into a digest."));

        var id = new DigestId(Guid.NewGuid());
        var digest = new Digest(id, userId, channel, window, scheduledSendUtc, nowUtc);

        digest.RaiseDomainEvent(new DigestScheduled(
            Guid.NewGuid(),
            id,
            userId,
            channel,
            window,
            scheduledSendUtc,
            nowUtc));

        return digest;
    }

    public Result Append(NotificationId notificationId, NotificationType type, string summary, string? actionUrl)
    {
        if (Status != DigestStatus.Open)
            return Result.Failure(new Error("Digest.Closed", "Cannot append items to a closed digest."));
        if (notificationId is null)
            return Result.Failure(new Error("Digest.NotificationRequired", "Notification ID is required."));
        if (string.IsNullOrWhiteSpace(summary))
            return Result.Failure(new Error("Digest.SummaryRequired", "Summary is required."));

        if (_items.Any(i => i.NotificationId == notificationId))
            return Result.Success(); // Idempotence check

        _items.Add(new DigestItem(
            Guid.NewGuid(),
            notificationId,
            type,
            summary.Trim(),
            actionUrl?.Trim(),
            DateTime.UtcNow));

        return Result.Success();
    }

    public void RemoveExpiredItems(DateTime cutoffUtc)
    {
        if (Status == DigestStatus.Open)
        {
            _items.RemoveAll(i => i.QueuedOnUtc < cutoffUtc);
        }
    }

    public Result Dispatch()
    {
        if (Status != DigestStatus.Open)
            return Result.Failure(new Error("Digest.Closed", "Only open digests can be dispatched."));

        if (!_items.Any())
        {
            Status = DigestStatus.Discarded;
        }
        else
        {
            Status = DigestStatus.Dispatched;
            DispatchedOnUtc = DateTime.UtcNow;
        }

        return Result.Success();
    }
}
