using System;
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

public record SendImmediateNotificationCommand(NotificationId NotificationId) : ICommand;

public class SendImmediateNotificationCommandHandler : ICommandHandler<SendImmediateNotificationCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IRecipientPreferencesRepository _prefsRepository;
    private readonly IEmailChannel _emailChannel;
    private readonly ISmsChannel _smsChannel;
    private readonly IRealtimePush _realtimePush;
    private readonly IDndScheduleCalculator _dndCalculator;
    private readonly IFrequencyCapEvaluator _smsCapEvaluator;
    private readonly IDncRegistry _dncRegistry;
    private readonly INotificationUnitOfWork _unitOfWork;

    public SendImmediateNotificationCommandHandler(
        INotificationRepository notificationRepository,
        IRecipientPreferencesRepository prefsRepository,
        IEmailChannel emailChannel,
        ISmsChannel smsChannel,
        IRealtimePush realtimePush,
        IDndScheduleCalculator dndCalculator,
        IFrequencyCapEvaluator smsCapEvaluator,
        IDncRegistry dncRegistry,
        INotificationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _prefsRepository = prefsRepository;
        _emailChannel = emailChannel;
        _smsChannel = smsChannel;
        _realtimePush = realtimePush;
        _dndCalculator = dndCalculator;
        _smsCapEvaluator = smsCapEvaluator;
        _dncRegistry = dncRegistry;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SendImmediateNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification == null)
            return Result.Failure(new Error("Notification.NotFound", "Notification not found."));

        if (notification.DeliveryStatus != DeliveryStatus.Pending && notification.DeliveryStatus != DeliveryStatus.Queued)
            return Result.Success(); // Already processed or sent

        var prefs = await _prefsRepository.GetByUserIdAsync(notification.RecipientUserId, cancellationToken);
        if (prefs == null)
            return Result.Failure(new Error("Preferences.NotFound", "Recipient preferences not found."));

        // 1. Enforce channel-specific compliance
        if (notification.Channel == Channel.Sms)
        {
            // Verify if SMS opt-in/suppression allows sending (bypass if critical security/transactional with High priority)
            bool isCritical = notification.Priority == Priority.High && 
                             (notification.Type == NotificationType.AccountSecurity || notification.Type == NotificationType.Transactional);
            
            if (!isCritical)
            {
                // DND / TCPA quiet hours check
                var checkWindowResult = _dndCalculator.CheckSmsSendWindow(prefs.Timezone, DateTime.UtcNow);
                if (checkWindowResult.IsSuccess && checkWindowResult.Value > DateTime.UtcNow)
                {
                    // Reschedule for next quiet hours release window
                    notification.HoldForDnd(checkWindowResult.Value);
                    _notificationRepository.Update(notification);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return Result.Success();
                }

                // Frequency cap check
                int smsSent = await _prefsRepository.CountSmsSentInLast24hAsync(notification.RecipientUserId, cancellationToken);
                var capResult = _smsCapEvaluator.CheckSmsCap(notification.RecipientUserId, smsSent, notification.Priority);
                if (capResult.IsFailure)
                {
                    // Reschedule for 24h later
                    notification.HoldForDnd(DateTime.UtcNow.AddDays(1));
                    _notificationRepository.Update(notification);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return capResult;
                }

                // National Do-Not-Call check
                if (prefs.SmsContact != null)
                {
                    bool isDnc = await _dncRegistry.IsRegisteredAsync(prefs.SmsContact.E164Number, cancellationToken);
                    if (isDnc)
                    {
                        prefs.SuppressSms("DncRegistryHit");
                        notification.RecordHardBounce("DncRegistryHit");
                        _prefsRepository.Update(prefs);
                        _notificationRepository.Update(notification);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        return Result.Failure(new Error("SMS.DncBlocked", "Number is registered on Do-Not-Call registry."));
                    }
                }
            }

            // Perform SMS delivery
            if (prefs.SmsContact == null)
            {
                notification.RecordHardBounce("NoSmsNumberOnFile");
                _notificationRepository.Update(notification);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure(new Error("SMS.NoPhone", "No verified phone number on file."));
            }

            var smsReq = new SmsSendRequest(prefs.SmsContact.E164Number, "Nexhire", notification.Rendered?.BodyText ?? "");
            var sendResult = await _smsChannel.SendAsync(smsReq, cancellationToken);
            if (sendResult.IsSuccess)
            {
                notification.RecordSendAttempt(sendResult.Value);
            }
            else
            {
                notification.RecordProviderError(sendResult.Error.Message);
            }
        }
        else if (notification.Channel == Channel.Email)
        {
            var emailReq = new EmailSendRequest(
                prefs.EmailContact.Address,
                "Nexhire Support",
                "no-reply@nexhire.com",
                "support@nexhire.com",
                notification.Rendered?.Subject ?? "Nexhire Notification",
                notification.Rendered?.BodyHtml ?? "",
                notification.Rendered?.BodyText ?? "",
                $"https://nexhire.com/unsubscribe?token={notification.RecipientUserId}",
                "House 12, Road 5, Banani, Dhaka, Bangladesh"
            );

            var sendResult = await _emailChannel.SendAsync(emailReq, cancellationToken);
            if (sendResult.IsSuccess)
            {
                notification.RecordSendAttempt(sendResult.Value);
            }
            else
            {
                notification.RecordProviderError(sendResult.Error.Message);
            }
        }
        else if (notification.Channel == Channel.InApp)
        {
            // For in-app we write to toast if enabled
            var cell = prefs.ChannelTypePrefs.FirstOrDefault(p => p.Channel == Channel.InApp && p.Type == notification.Type);
            bool pushToast = cell == null || cell.ToastMode == ToastMode.Toast;

            string providerId = Guid.NewGuid().ToString();
            
            if (pushToast)
            {
                var toast = new InAppToastDto(
                    notification.Id.Value,
                    notification.Type.ToString(),
                    notification.Rendered?.Subject ?? "Notification",
                    notification.Rendered?.BodyText ?? "",
                    null,
                    notification.Priority.ToString()
                );
                
                await _realtimePush.PushToastAsync(notification.RecipientUserId, toast, cancellationToken);
            }

            notification.RecordSendAttempt(providerId);
            notification.MarkDelivered(); // In-app is immediately delivered to center
        }

        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
