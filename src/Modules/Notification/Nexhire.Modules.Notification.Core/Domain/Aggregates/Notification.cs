using System;
using System.Collections.Generic;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.Notification.Core.Domain.Events;

namespace Nexhire.Modules.Notification.Core.Domain.Aggregates;

public sealed class DeliveryAttempt : Entity<Guid>
{
    public int AttemptNumber { get; private set; }
    public Channel Channel { get; private set; }
    public AttemptOutcome Outcome { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? ProviderResponse { get; private set; }
    public DateTime AttemptedOnUtc { get; private set; }

    private DeliveryAttempt() { } // EF Core

    internal DeliveryAttempt(
        Guid id,
        int attemptNumber,
        Channel channel,
        AttemptOutcome outcome,
        string? providerMessageId,
        string? providerResponse,
        DateTime attemptedOnUtc) : base(id)
    {
        AttemptNumber = attemptNumber;
        Channel = channel;
        Outcome = outcome;
        ProviderMessageId = providerMessageId;
        ProviderResponse = providerResponse;
        AttemptedOnUtc = attemptedOnUtc;
    }
}

public sealed class Notification : AggregateRoot<NotificationId>
{
    private readonly List<DeliveryAttempt> _attempts = new();

    public Guid RecipientUserId { get; private set; }
    public Channel Channel { get; private set; }
    public NotificationType Type { get; private set; }
    public Priority Priority { get; private set; }
    public SourceEventRef SourceEvent { get; private set; } = null!;
    public NotificationPayload Payload { get; private set; } = null!;
    public Guid? TemplateId { get; private set; }
    public RenderedMessage? Rendered { get; private set; }
    public DeliveryStatus DeliveryStatus { get; private set; }
    public EngagementState Engagement { get; private set; } = null!;
    public DateTime? ScheduledForUtc { get; private set; }
    public bool IsRead { get; private set; }
    public bool IsArchived { get; private set; }
    public Guid? DigestId { get; private set; }
    public string? ProviderMessageId { get; private set; } // for easy lookup in EF Core
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }
    public int VersionToken { get; private set; }

    public IReadOnlyCollection<DeliveryAttempt> Attempts => _attempts.AsReadOnly();

    private Notification() { } // EF Core

    private Notification(
        NotificationId id,
        Guid recipientUserId,
        Channel channel,
        NotificationType type,
        Priority priority,
        SourceEventRef sourceEvent,
        NotificationPayload payload,
        DateTime nowUtc) : base(id)
    {
        RecipientUserId = recipientUserId;
        Channel = channel;
        Type = type;
        Priority = priority;
        SourceEvent = sourceEvent;
        Payload = payload;
        DeliveryStatus = DeliveryStatus.Pending;
        Engagement = EngagementState.Create().Value;
        IsRead = false;
        IsArchived = false;
        CreatedOnUtc = nowUtc;
        UpdatedOnUtc = nowUtc;
        VersionToken = 1;
    }

    public static Result<Notification> Create(
        Guid recipientUserId,
        Channel channel,
        NotificationType type,
        Priority priority,
        SourceEventRef sourceEvent,
        NotificationPayload payload,
        DateTime nowUtc)
    {
        if (recipientUserId == Guid.Empty)
            return Result.Failure<Notification>(new Error("Notification.UserRequired", "Recipient User ID is required."));
        if (sourceEvent is null)
            return Result.Failure<Notification>(new Error("Notification.SourceEventRequired", "Source event ref is required."));
        if (payload is null)
            return Result.Failure<Notification>(new Error("Notification.PayloadRequired", "Payload is required."));

        var id = NotificationId.New();
        var notification = new Notification(id, recipientUserId, channel, type, priority, sourceEvent, payload, nowUtc);
        
        notification.RaiseDomainEvent(new NotificationCreated(
            Guid.NewGuid(),
            id,
            recipientUserId,
            channel,
            type,
            priority,
            nowUtc));

        return notification;
    }

    public Result Render(TemplateVersion templateVersion, RenderedMessage rendered)
    {
        if (DeliveryStatus != DeliveryStatus.Pending && DeliveryStatus != DeliveryStatus.Queued)
            return Result.Failure(new Error("Notification.WrongStatus", "Notification cannot be rendered unless Pending or Queued."));
        if (templateVersion is null)
            return Result.Failure(new Error("Notification.TemplateRequired", "Template version is required for rendering."));
        if (rendered is null)
            return Result.Failure(new Error("Notification.RenderedRequired", "Rendered message is required."));

        TemplateId = templateVersion.CreatedByUserId; // Representing template system reference
        Rendered = rendered;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result QueueIntoDigest(Guid digestId, DateTime scheduledSendUtc)
    {
        if (Priority == Priority.High)
            return Result.Failure(new Error("Notification.HighPriorityDigest", "High-priority notifications cannot be batched into a digest."));
        if (DeliveryStatus != DeliveryStatus.Pending)
            return Result.Failure(new Error("Notification.WrongStatus", "Notification can only be queued from Pending."));

        DigestId = digestId;
        DeliveryStatus = DeliveryStatus.Queued;
        ScheduledForUtc = scheduledSendUtc;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result HoldForDnd(DateTime releaseAtUtc)
    {
        if (Priority == Priority.High)
            return Result.Failure(new Error("Notification.HighPriorityDnd", "High-priority notifications cannot be held for DND."));
        if (DeliveryStatus != DeliveryStatus.Pending && DeliveryStatus != DeliveryStatus.Queued)
            return Result.Failure(new Error("Notification.WrongStatus", "Notification must be Pending or Queued to hold for DND."));

        ScheduledForUtc = releaseAtUtc;
        DeliveryStatus = DeliveryStatus.Queued;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkQueued()
    {
        if (DeliveryStatus != DeliveryStatus.Pending)
            return Result.Failure(new Error("Notification.WrongStatus", "Notification can only be marked Queued from Pending."));

        DeliveryStatus = DeliveryStatus.Queued;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RecordSendAttempt(string providerMessageId)
    {
        if (Channel != Channel.InApp && Rendered == null)
            return Result.Failure(new Error("Notification.NotRendered", "Notification must be rendered before sending."));
        if (DeliveryStatus != DeliveryStatus.Pending && DeliveryStatus != DeliveryStatus.Queued)
            return Result.Failure(new Error("Notification.WrongStatus", "Notification must be Pending or Queued to send."));

        int attemptNo = _attempts.Count + 1;
        _attempts.Add(new DeliveryAttempt(
            Guid.NewGuid(),
            attemptNo,
            Channel,
            AttemptOutcome.Succeeded,
            providerMessageId,
            "Success",
            DateTime.UtcNow));

        ProviderMessageId = providerMessageId;
        DeliveryStatus = DeliveryStatus.Sent;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RecordSoftBounce(string reason)
    {
        int attemptNo = _attempts.Count + 1;
        if (attemptNo > 3)
            return Result.Failure(new Error("Notification.MaxAttemptsExceeded", "Maximum attempts exceeded."));

        var outcome = AttemptOutcome.SoftBounce;
        _attempts.Add(new DeliveryAttempt(
            Guid.NewGuid(),
            attemptNo,
            Channel,
            outcome,
            ProviderMessageId,
            reason,
            DateTime.UtcNow));

        if (attemptNo == 3)
        {
            DeliveryStatus = DeliveryStatus.Failed;
        }
        else
        {
            DeliveryStatus = DeliveryStatus.Sent; // Eligible for retry
        }

        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RecordHardBounce(string reason)
    {
        int attemptNo = _attempts.Count + 1;
        _attempts.Add(new DeliveryAttempt(
            Guid.NewGuid(),
            attemptNo,
            Channel,
            AttemptOutcome.HardBounce,
            ProviderMessageId,
            reason,
            DateTime.UtcNow));

        DeliveryStatus = DeliveryStatus.Bounced;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RecordProviderError(string reason)
    {
        int attemptNo = _attempts.Count + 1;
        _attempts.Add(new DeliveryAttempt(
            Guid.NewGuid(),
            attemptNo,
            Channel,
            AttemptOutcome.ProviderError,
            ProviderMessageId,
            reason,
            DateTime.UtcNow));

        if (attemptNo >= 3)
        {
            DeliveryStatus = DeliveryStatus.Failed;
        }
        else
        {
            DeliveryStatus = DeliveryStatus.Sent; // Eligible for retry
        }

        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkDelivered()
    {
        if (DeliveryStatus != DeliveryStatus.Sent)
            return Result.Failure(new Error("Notification.WrongStatus", "Notification must be in Sent status to mark delivered."));

        DeliveryStatus = DeliveryStatus.Delivered;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkOpened()
    {
        if (Channel == Channel.Sms)
            return Result.Failure(new Error("Notification.WrongChannel", "SMS notifications cannot be marked opened."));

        var result = EngagementState.Create(DateTime.UtcNow, Engagement.ClickedOnUtc);
        if (result.IsFailure) return result;

        Engagement = result.Value;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkClicked()
    {
        var openedTime = Engagement.OpenedOnUtc ?? DateTime.UtcNow;
        var result = EngagementState.Create(openedTime, DateTime.UtcNow);
        if (result.IsFailure) return result;

        Engagement = result.Value;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkComplaint()
    {
        DeliveryStatus = DeliveryStatus.Complaint;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkRead()
    {
        if (Channel != Channel.InApp)
            return Result.Failure(new Error("E-NOTIF-WRONG-CHANNEL", "Only InApp notifications can be marked read."));

        if (!IsRead)
        {
            IsRead = true;
            UpdatedOnUtc = DateTime.UtcNow;
            RaiseDomainEvent(new NotificationRead(Guid.NewGuid(), Id, RecipientUserId, DateTime.UtcNow));
        }

        return Result.Success();
    }

    public Result Archive()
    {
        if (Channel != Channel.InApp)
            return Result.Failure(new Error("E-NOTIF-WRONG-CHANNEL", "Only InApp notifications can be archived."));

        if (!IsArchived)
        {
            IsArchived = true;
            UpdatedOnUtc = DateTime.UtcNow;
            RaiseDomainEvent(new NotificationArchived(Guid.NewGuid(), Id, RecipientUserId, DateTime.UtcNow));
        }

        return Result.Success();
    }

    public Result Unarchive()
    {
        if (Channel != Channel.InApp)
            return Result.Failure(new Error("E-NOTIF-WRONG-CHANNEL", "Only InApp notifications can be unarchived."));

        if (IsArchived)
        {
            IsArchived = false;
            UpdatedOnUtc = DateTime.UtcNow;
        }

        return Result.Success();
    }
}
