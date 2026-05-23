using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.Notification.Core.Domain;
using Nexhire.Modules.Notification.Core.Domain.Aggregates;
using Nexhire.Modules.Notification.Core.Domain.Ports;
using Nexhire.Modules.Notification.Core.Domain.Repositories;
using Nexhire.Modules.Notification.Core.Domain.Services;

namespace Nexhire.Modules.Notification.Core.CQRS.Commands;

public record RecordDeliveryStatusCommand(
    string ProviderMessageId,
    string Status,
    string Reason = "") : ICommand;

public record DispatchDigestCommand(Guid DigestId) : ICommand;

public class RecordDeliveryStatusCommandHandler : ICommandHandler<RecordDeliveryStatusCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IRecipientPreferencesRepository _prefsRepository;
    private readonly INotificationUnitOfWork _unitOfWork;

    public RecordDeliveryStatusCommandHandler(
        INotificationRepository notificationRepository,
        IRecipientPreferencesRepository prefsRepository,
        INotificationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _prefsRepository = prefsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RecordDeliveryStatusCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByProviderMessageIdAsync(request.ProviderMessageId, cancellationToken);
        if (notification == null)
            return Result.Success(); // Silent drop if webhook arrives for unknown provider message (Conformist resilience)

        var statusClean = request.Status.Trim().ToLowerInvariant();
        if (statusClean == "delivered")
        {
            notification.MarkDelivered();
        }
        else if (statusClean == "bounced")
        {
            notification.RecordHardBounce(request.Reason);
            
            // Hard bounce suppression rule:
            var prefs = await _prefsRepository.GetByUserIdAsync(notification.RecipientUserId, cancellationToken);
            if (prefs != null)
            {
                if (notification.Channel == Channel.Email)
                {
                    // Suppress email on 3 bounces or direct hard bounce
                    prefs.SuppressEmail("ProviderHardBounce");
                }
                else if (notification.Channel == Channel.Sms)
                {
                    prefs.SuppressSms("ProviderHardBounce");
                }
                _prefsRepository.Update(prefs);
            }
        }
        else if (statusClean == "softbounce")
        {
            notification.RecordSoftBounce(request.Reason);
            if (notification.DeliveryStatus == DeliveryStatus.Failed)
            {
                // Suppress after 3 failures
                var prefs = await _prefsRepository.GetByUserIdAsync(notification.RecipientUserId, cancellationToken);
                if (prefs != null)
                {
                    if (notification.Channel == Channel.Email) prefs.SuppressEmail("SoftBounceExhausted");
                    else if (notification.Channel == Channel.Sms) prefs.SuppressSms("SoftBounceExhausted");
                    _prefsRepository.Update(prefs);
                }
            }
        }
        else if (statusClean == "complaint")
        {
            notification.MarkComplaint();
            var prefs = await _prefsRepository.GetByUserIdAsync(notification.RecipientUserId, cancellationToken);
            if (prefs != null)
            {
                if (notification.Channel == Channel.Email) prefs.SuppressEmail("SpamComplaint");
                else if (notification.Channel == Channel.Sms) prefs.SuppressSms("SpamComplaint");
                _prefsRepository.Update(prefs);
            }
        }
        else if (statusClean == "opened")
        {
            notification.MarkOpened();
        }
        else if (statusClean == "clicked")
        {
            notification.MarkClicked();
        }

        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class DispatchDigestCommandHandler : ICommandHandler<DispatchDigestCommand>
{
    private readonly IDigestRepository _digestRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly IRecipientPreferencesRepository _prefsRepository;
    private readonly IEmailChannel _emailChannel;
    private readonly IDigestAssembler _assembler;
    private readonly INotificationUnitOfWork _unitOfWork;

    public DispatchDigestCommandHandler(
        IDigestRepository digestRepository,
        INotificationRepository notificationRepository,
        INotificationTemplateRepository templateRepository,
        IRecipientPreferencesRepository prefsRepository,
        IEmailChannel emailChannel,
        IDigestAssembler assembler,
        INotificationUnitOfWork unitOfWork)
    {
        _digestRepository = digestRepository;
        _notificationRepository = notificationRepository;
        _templateRepository = templateRepository;
        _prefsRepository = prefsRepository;
        _emailChannel = emailChannel;
        _assembler = assembler;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DispatchDigestCommand request, CancellationToken cancellationToken)
    {
        var digest = await _digestRepository.GetOpenAsync(
            Guid.Empty, Channel.Email, DigestWindow.Daily, cancellationToken); // Simple lookup stub or standard find
        // Instead of Guid.Empty let's find by actual Id
        var dueDigests = await _digestRepository.GetDueAsync(DateTime.UtcNow, cancellationToken);
        var targetDigest = dueDigests.FirstOrDefault(d => d.Id.Value == request.DigestId);
        if (targetDigest == null)
            return Result.Failure(new Error("Digest.NotFound", "Digest not found or already dispatched."));

        targetDigest.RemoveExpiredItems(DateTime.UtcNow.AddDays(-30));
        
        if (targetDigest.Items.Count == 0)
        {
            targetDigest.Dispatch(); // transitions to Discarded
            _digestRepository.Update(targetDigest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(); // Empty digest is not sent per AC
        }

        var prefs = await _prefsRepository.GetByUserIdAsync(targetDigest.UserId, cancellationToken);
        if (prefs == null || prefs.EmailSuppressed || prefs.GlobalEmailOptOut)
        {
            targetDigest.Dispatch(); // discard or silent drop
            _digestRepository.Update(targetDigest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // Resolve digest template
        // Digests use a specific announcement or notification type template
        var template = await _templateRepository.GetByChannelAndTypeAsync(targetDigest.Channel, NotificationType.Announcement, cancellationToken);
        if (template == null)
        {
            // Create a default digest template if missing
            var initVersion = TemplateVersion.Create(
                1,
                "Your Nexhire Digest: {{digest.count}} Updates",
                "<h2>Nexhire Updates</h2><p>Here are your batched updates:</p><div>{{digest.items}}</div>",
                "Nexhire Updates\nHere are your batched updates:\n{{digest.items}}",
                "",
                new List<string> { "digest.count", "digest.items" },
                DateTime.UtcNow,
                Guid.Empty).Value;
            
            template = NotificationTemplate.Create(targetDigest.Channel, NotificationType.Announcement, "Digest Template", initVersion, DateTime.UtcNow).Value;
            await _templateRepository.AddAsync(template, cancellationToken);
        }

        // Fetch notifications payloads for the digest items to enrich template substitution
        var itemPayloads = new List<NotificationPayload>();
        foreach (var item in targetDigest.Items)
        {
            var notif = await _notificationRepository.GetByIdAsync(item.NotificationId, cancellationToken);
            if (notif != null)
            {
                itemPayloads.Add(notif.Payload);
            }
            else
            {
                itemPayloads.Add(NotificationPayload.Create(new()).Value);
            }
        }

        var renderResult = _assembler.Assemble(targetDigest, template, itemPayloads);
        if (renderResult.IsFailure) return renderResult;

        var rendered = renderResult.Value;

        // Perform actual send
        var emailReq = new EmailSendRequest(
            prefs.EmailContact.Address,
            "Nexhire Digests",
            "no-reply@nexhire.com",
            "support@nexhire.com",
            rendered.Subject ?? "Your Nexhire Digest",
            rendered.BodyHtml ?? "",
            rendered.BodyText,
            $"https://nexhire.com/unsubscribe?token={targetDigest.UserId}",
            "House 12, Road 5, Banani, Dhaka, Bangladesh"
        );

        var sendResult = await _emailChannel.SendAsync(emailReq, cancellationToken);
        
        targetDigest.Dispatch(); // mark dispatched
        
        // Mark all queued notifications as Sent
        foreach (var item in targetDigest.Items)
        {
            var notif = await _notificationRepository.GetByIdAsync(item.NotificationId, cancellationToken);
            if (notif != null)
            {
                if (sendResult.IsSuccess)
                {
                    notif.RecordSendAttempt(sendResult.Value);
                    notif.MarkDelivered();
                }
                else
                {
                    notif.RecordProviderError(sendResult.Error.Message);
                }
                _notificationRepository.Update(notif);
            }
        }

        _digestRepository.Update(targetDigest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
