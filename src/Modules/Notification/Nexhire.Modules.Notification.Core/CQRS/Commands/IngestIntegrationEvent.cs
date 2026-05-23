using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.Notification.Core.Domain.Aggregates;
using Nexhire.Modules.Notification.Core.Domain.Repositories;
using Nexhire.Modules.Notification.Core.Domain.Services;
using Nexhire.Modules.Notification.Core.Domain;
using NotificationAggregate = Nexhire.Modules.Notification.Core.Domain.Aggregates.Notification;

namespace Nexhire.Modules.Notification.Core.CQRS.Commands;

public record IngestIntegrationEventCommand(
    Guid EventId,
    string EventType,
    string SourceBc,
    Dictionary<string, string> Payload,
    string PriorityString = "") : ICommand;

public class IngestIntegrationEventCommandHandler : ICommandHandler<IngestIntegrationEventCommand>
{
    private readonly IRecipientPreferencesRepository _prefsRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly IDigestRepository _digestRepository;
    private readonly IChannelFanoutPlanner _planner;
    private readonly ITemplateRenderer _renderer;
    private readonly IDndScheduleCalculator _dndCalculator;
    private readonly INotificationUnitOfWork _unitOfWork;

    public IngestIntegrationEventCommandHandler(
        IRecipientPreferencesRepository prefsRepository,
        INotificationRepository notificationRepository,
        INotificationTemplateRepository templateRepository,
        IDigestRepository digestRepository,
        IChannelFanoutPlanner planner,
        ITemplateRenderer renderer,
        IDndScheduleCalculator dndCalculator,
        INotificationUnitOfWork unitOfWork)
    {
        _prefsRepository = prefsRepository;
        _notificationRepository = notificationRepository;
        _templateRepository = templateRepository;
        _digestRepository = digestRepository;
        _planner = planner;
        _renderer = renderer;
        _dndCalculator = dndCalculator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(IngestIntegrationEventCommand request, CancellationToken cancellationToken)
    {
        // 1. Extract recipient user ID from the event payload. 
        // Upstreams have different names for it, so we inspect common keys (e.g. JobSeekerUserId, EmployerUserId, UserId)
        Guid recipientUserId = Guid.Empty;
        if (request.Payload.TryGetValue("JobSeekerUserId", out string? jsIdStr) && Guid.TryParse(jsIdStr, out var jsId))
            recipientUserId = jsId;
        else if (request.Payload.TryGetValue("EmployerUserId", out string? empIdStr) && Guid.TryParse(empIdStr, out var empId))
            recipientUserId = empId;
        else if (request.Payload.TryGetValue("UserId", out string? usrIdStr) && Guid.TryParse(usrIdStr, out var usrId))
            recipientUserId = usrId;

        if (recipientUserId == Guid.Empty)
        {
            // If recipient ID cannot be resolved, log and drop to remain resilient (Conformist rule)
            return Result.Success();
        }

        // 2. Load or create default RecipientPreferences
        var prefs = await _prefsRepository.GetByUserIdAsync(recipientUserId, cancellationToken);
        if (prefs == null)
        {
            string email = request.Payload.TryGetValue("Email", out string? emailStr) ? emailStr : $"{recipientUserId}@nexhire.com";
            string role = request.Payload.TryGetValue("Role", out string? roleStr) ? roleStr : "Candidate";
            
            var emailVo = EmailContactPoint.Create(email).Value;
            prefs = RecipientPreferences.CreateDefault(recipientUserId, role, emailVo, DateTime.UtcNow).Value;
            await _prefsRepository.AddAsync(prefs, cancellationToken);
        }

        // Deactivated user check: treat as global opt-out
        if (request.EventType.Equals("AccountDeactivatedIntegrationEvent", StringComparison.OrdinalIgnoreCase))
        {
            prefs.SetGlobalEmailOptOut(true, "AccountDeactivatedEvent", null);
        }

        // 3. Plan fan-out
        var now = DateTime.UtcNow;
        var plannedNotifications = _planner.Plan(request.EventType, request.PriorityString, prefs, now);
        if (plannedNotifications.Count == 0)
        {
            return Result.Success();
        }

        var sourceEvent = SourceEventRef.Create(request.EventId, request.EventType, request.SourceBc).Value;
        var payload = NotificationPayload.Create(request.Payload).Value;

        // 4. Create and route each planned notification
        foreach (var planned in plannedNotifications)
        {
            var notification = NotificationAggregate.Create(
                recipientUserId,
                planned.Channel,
                planned.Type,
                planned.Priority,
                sourceEvent,
                payload,
                now).Value;

            if (planned.HeldForDnd)
            {
                var releaseUtc = _dndCalculator.NextReleaseTimeUtc(prefs.DoNotDisturb!, prefs.Timezone, now) ?? now.AddHours(8);
                notification.HoldForDnd(releaseUtc);
                await _notificationRepository.AddAsync(notification, cancellationToken);
            }
            else if (planned.Frequency == Frequency.DailyDigest || planned.Frequency == Frequency.WeeklyDigest)
            {
                var window = planned.Frequency == Frequency.DailyDigest ? DigestWindow.Daily : DigestWindow.Weekly;
                
                // Calculate next scheduled send window boundary
                var tz = TimeZoneInfo.FindSystemTimeZoneById(prefs.Timezone);
                var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, tz);
                
                // Digest time: daily 08:00 or weekly Monday 08:00 local
                var localSend = localNow.Date.AddHours(8);
                if (window == DigestWindow.Weekly)
                {
                    int daysToMonday = ((int)DayOfWeek.Monday - (int)localNow.DayOfWeek + 7) % 7;
                    if (daysToMonday == 0 && localNow.Hour >= 8) daysToMonday = 7;
                    localSend = localNow.Date.AddDays(daysToMonday).AddHours(8);
                }
                else if (localNow.Hour >= 8)
                {
                    localSend = localSend.AddDays(1);
                }
                
                var scheduledSendUtc = TimeZoneInfo.ConvertTimeToUtc(localSend, tz);

                var digest = await _digestRepository.GetOpenAsync(recipientUserId, planned.Channel, window, cancellationToken);
                if (digest == null)
                {
                    digest = Digest.Open(recipientUserId, planned.Channel, window, scheduledSendUtc, now).Value;
                    await _digestRepository.AddAsync(digest, cancellationToken);
                }

                string summary = request.Payload.TryGetValue("Title", out string? title) ? title : "Notification Update";
                string? actionUrl = request.Payload.TryGetValue("ActionUrl", out string? url) ? url : null;

                digest.Append(notification.Id, planned.Type, summary, actionUrl);
                notification.QueueIntoDigest(digest.Id.Value, scheduledSendUtc);
                
                await _notificationRepository.AddAsync(notification, cancellationToken);
                _digestRepository.Update(digest);
            }
            else
            {
                // Immediate Send path
                if (planned.Channel == Channel.InApp)
                {
                    // In-app defaults to inline rendering using payload
                    var rendered = RenderedMessage.Create(
                        request.Payload.TryGetValue("Title", out string? t) ? t : "Update",
                        request.Payload.TryGetValue("BodyHtml", out string? h) ? h : null,
                        request.Payload.TryGetValue("BodyText", out string? b) ? b : "Update description"
                    ).Value;

                    notification.Render(
                        TemplateVersion.Create(1, "InApp", "InApp", "InApp", "InApp", new(), now, Guid.Empty).Value, 
                        rendered);
                }
                else
                {
                    // Email/SMS template resolution
                    var template = await _templateRepository.GetByChannelAndTypeAsync(planned.Channel, planned.Type, cancellationToken);
                    if (template != null && template.IsActive)
                    {
                        var renderedResult = _renderer.Render(template.CurrentVersion, payload, planned.Channel);
                        if (renderedResult.IsSuccess)
                        {
                            notification.Render(template.CurrentVersion, renderedResult.Value);
                        }
                    }
                    else
                    {
                        // Fallback to a built-in rendering if template is missing/inactive
                        var fallbackVersion = TemplateVersion.Create(
                            1,
                            "Notification",
                            "<p>An update has occurred: {{Title}}</p>",
                            "An update has occurred: {{Title}}",
                            "",
                            new List<string> { "Title" },
                            now,
                            Guid.Empty).Value;
                        
                        var renderedResult = _renderer.Render(fallbackVersion, payload, planned.Channel);
                        notification.Render(fallbackVersion, renderedResult.Value);
                    }
                }

                await _notificationRepository.AddAsync(notification, cancellationToken);
            }
        }

        // 5. Commit unit of work
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
