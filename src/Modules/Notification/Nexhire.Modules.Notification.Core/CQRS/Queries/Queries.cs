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
using Nexhire.Modules.Notification.Core.Domain.Repositories;
using Nexhire.Modules.Notification.Core.Domain.Services;

namespace Nexhire.Modules.Notification.Core.CQRS.Queries;

// Queries definitions
public record GetNotificationCenterQuery(Guid UserId, int PageSize = 20, int PageNumber = 1) : IQuery<NotificationCenterDto>;

public record GetNotificationHistoryQuery(
    Guid UserId, 
    string? FilterType = null, 
    string? SearchTerm = null, 
    int Skip = 0, 
    int Take = 20) : IQuery<List<NotificationDto>>;

public record GetUnreadCountQuery(Guid UserId) : IQuery<int>;

public record GetMyNotificationPreferencesQuery(Guid UserId) : IQuery<NotificationPreferencesDto>;

public record PreviewTemplateQuery(
    Guid TemplateId, 
    int VersionNumber, 
    Dictionary<string, string> SamplePayload) : IQuery<RenderedMessageDto>;

public record GetTemplateQuery(Guid TemplateId) : IQuery<TemplateDto>;

public record ListTemplatesQuery() : IQuery<List<TemplateDto>>;

public record GetDeliveryLogQuery(
    Guid? UserId, 
    DateTime? FromUtc, 
    DateTime? ToUtc, 
    string? ChannelString, 
    string? StatusString, 
    int Skip = 0, 
    int Take = 50) : IQuery<List<NotificationLogEntryDto>>;

// DTOs
public record NotificationDto(
    Guid Id,
    string Channel,
    string Type,
    string Priority,
    string Subject,
    string BodyText,
    bool IsRead,
    DateTime CreatedOnUtc);

public record NotificationCenterDto(
    List<NotificationDto> Notifications,
    int UnreadCount,
    bool HasMore);

public record ChannelPreferenceGridItemDto(
    string Channel,
    string Type,
    bool Enabled,
    string Frequency,
    string ToastMode);

public record NotificationPreferencesDto(
    Guid UserId,
    string PreferredEmail,
    string? PhoneNumber,
    bool SmsVerified,
    bool SmsOptedIn,
    string Timezone,
    TimeOnly? DndStart,
    TimeOnly? DndEnd,
    bool GlobalEmailOptOut,
    List<ChannelPreferenceGridItemDto> Preferences);

public record RenderedMessageDto(
    string? Subject,
    string? BodyHtml,
    string BodyText);

public record TemplateVersionDto(
    int VersionNo,
    string? Subject,
    string BodyHtml,
    string BodyText,
    string Footer,
    List<string> Placeholders,
    DateTime CreatedOnUtc);

public record TemplateDto(
    Guid Id,
    string Channel,
    string Type,
    string Name,
    bool IsActive,
    TemplateVersionDto CurrentVersion,
    List<TemplateVersionDto> History);

// Handlers
public class QueriesHandler :
    IQueryHandler<GetNotificationCenterQuery, NotificationCenterDto>,
    IQueryHandler<GetNotificationHistoryQuery, List<NotificationDto>>,
    IQueryHandler<GetUnreadCountQuery, int>,
    IQueryHandler<GetMyNotificationPreferencesQuery, NotificationPreferencesDto>,
    IQueryHandler<PreviewTemplateQuery, RenderedMessageDto>,
    IQueryHandler<GetTemplateQuery, TemplateDto>,
    IQueryHandler<ListTemplatesQuery, List<TemplateDto>>,
    IQueryHandler<GetDeliveryLogQuery, List<NotificationLogEntryDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IRecipientPreferencesRepository _prefsRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationLogRepository _logRepository;
    private readonly ITemplateRenderer _renderer;

    public QueriesHandler(
        INotificationRepository notificationRepository,
        IRecipientPreferencesRepository prefsRepository,
        INotificationTemplateRepository templateRepository,
        INotificationLogRepository logRepository,
        ITemplateRenderer renderer)
    {
        _notificationRepository = notificationRepository;
        _prefsRepository = prefsRepository;
        _templateRepository = templateRepository;
        _logRepository = logRepository;
        _renderer = renderer;
    }

    public async Task<Result<NotificationCenterDto>> Handle(GetNotificationCenterQuery request, CancellationToken cancellationToken)
    {
        int skip = (request.PageNumber - 1) * request.PageSize;
        var list = await _notificationRepository.GetInAppForUserAsync(request.UserId, skip, request.PageSize + 1, cancellationToken);
        int unread = await _notificationRepository.CountUnreadForUserAsync(request.UserId, cancellationToken);

        bool hasMore = list.Count > request.PageSize;
        var dtos = list.Take(request.PageSize).Select(n => new NotificationDto(
            n.Id.Value,
            n.Channel.ToString(),
            n.Type.ToString(),
            n.Priority.ToString(),
            n.Rendered?.Subject ?? "Update",
            n.Rendered?.BodyText ?? "Update details",
            n.IsRead,
            n.CreatedOnUtc
        )).ToList();

        return new NotificationCenterDto(dtos, unread, hasMore);
    }

    public async Task<Result<List<NotificationDto>>> Handle(GetNotificationHistoryQuery request, CancellationToken cancellationToken)
    {
        // Simple search query or retrieval of historical notifications
        var list = await _notificationRepository.GetInAppForUserAsync(request.UserId, request.Skip, request.Take, cancellationToken);
        
        var queryable = list.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.FilterType) && Enum.TryParse<NotificationType>(request.FilterType, true, out var type))
        {
            queryable = queryable.Where(n => n.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            string term = request.SearchTerm.ToLowerInvariant();
            queryable = queryable.Where(n => 
                (n.Rendered?.Subject != null && n.Rendered.Subject.ToLowerInvariant().Contains(term)) ||
                (n.Rendered?.BodyText != null && n.Rendered.BodyText.ToLowerInvariant().Contains(term)));
        }

        var dtos = queryable.Select(n => new NotificationDto(
            n.Id.Value,
            n.Channel.ToString(),
            n.Type.ToString(),
            n.Priority.ToString(),
            n.Rendered?.Subject ?? "Update",
            n.Rendered?.BodyText ?? "Update details",
            n.IsRead,
            n.CreatedOnUtc
        )).ToList();

        return dtos;
    }

    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        int count = await _notificationRepository.CountUnreadForUserAsync(request.UserId, cancellationToken);
        return count;
    }

    public async Task<Result<NotificationPreferencesDto>> Handle(GetMyNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var prefs = await _prefsRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (prefs == null)
            return Result.Failure<NotificationPreferencesDto>(new Error("Preferences.NotFound", "Preferences not found."));

        var grid = prefs.ChannelTypePrefs.Select(p => new ChannelPreferenceGridItemDto(
            p.Channel.ToString(),
            p.Type.ToString(),
            p.Enabled,
            p.Frequency.ToString(),
            p.ToastMode.ToString()
        )).ToList();

        var dto = new NotificationPreferencesDto(
            prefs.UserId,
            prefs.EmailContact.Address,
            prefs.SmsContact?.E164Number,
            prefs.SmsContact?.Verified ?? false,
            prefs.SmsContact?.OptedIn ?? false,
            prefs.Timezone,
            prefs.DoNotDisturb?.StartLocalTime,
            prefs.DoNotDisturb?.EndLocalTime,
            prefs.GlobalEmailOptOut,
            grid
        );

        return dto;
    }

    public async Task<Result<RenderedMessageDto>> Handle(PreviewTemplateQuery request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(new NotificationTemplateId(request.TemplateId), cancellationToken);
        if (template == null)
            return Result.Failure<RenderedMessageDto>(new Error("Template.NotFound", "Template not found."));

        TemplateVersion? target = null;
        if (template.CurrentVersion.VersionNumber == request.VersionNumber)
        {
            target = template.CurrentVersion;
        }
        else
        {
            target = template.History.FirstOrDefault(v => v.VersionNumber == request.VersionNumber);
        }

        if (target == null)
            return Result.Failure<RenderedMessageDto>(new Error("Template.VersionNotFound", "Template version not found."));

        var payload = NotificationPayload.Create(request.SamplePayload).Value;
        var renderResult = _renderer.Render(target, payload, template.Channel);
        if (renderResult.IsFailure) return Result.Failure<RenderedMessageDto>(renderResult.Error);

        var rendered = renderResult.Value;
        return new RenderedMessageDto(rendered.Subject, rendered.BodyHtml, rendered.BodyText);
    }

    public async Task<Result<TemplateDto>> Handle(GetTemplateQuery request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(new NotificationTemplateId(request.TemplateId), cancellationToken);
        if (template == null)
            return Result.Failure<TemplateDto>(new Error("Template.NotFound", "Template not found."));

        return MapTemplate(template);
    }

    public async Task<Result<List<TemplateDto>>> Handle(ListTemplatesQuery request, CancellationToken cancellationToken)
    {
        var list = await _templateRepository.ListAllAsync(cancellationToken);
        return list.Select(MapTemplate).ToList();
    }

    public async Task<Result<List<NotificationLogEntryDto>>> Handle(GetDeliveryLogQuery request, CancellationToken cancellationToken)
    {
        Channel? chan = null;
        if (!string.IsNullOrWhiteSpace(request.ChannelString) && Enum.TryParse<Channel>(request.ChannelString, true, out var parsedChan))
            chan = parsedChan;

        DeliveryStatus? stat = null;
        if (!string.IsNullOrWhiteSpace(request.StatusString) && Enum.TryParse<DeliveryStatus>(request.StatusString, true, out var parsedStat))
            stat = parsedStat;

        var logs = await _logRepository.QueryLogsAsync(
            request.UserId,
            request.FromUtc,
            request.ToUtc,
            chan,
            stat,
            request.Skip,
            request.Take,
            cancellationToken);

        return logs;
    }

    private static TemplateDto MapTemplate(NotificationTemplate t)
    {
        var curVer = new TemplateVersionDto(
            t.CurrentVersion.VersionNumber,
            t.CurrentVersion.Subject,
            t.CurrentVersion.BodyHtml,
            t.CurrentVersion.BodyText,
            t.CurrentVersion.Footer,
            t.CurrentVersion.Placeholders,
            t.CurrentVersion.CreatedOnUtc
        );

        var hist = t.History.Select(v => new TemplateVersionDto(
            v.VersionNumber,
            v.Subject,
            v.BodyHtml,
            v.BodyText,
            v.Footer,
            v.Placeholders,
            v.CreatedOnUtc
        )).ToList();

        return new TemplateDto(
            t.Id.Value,
            t.Channel.ToString(),
            t.Type.ToString(),
            t.Name,
            t.IsActive,
            curVer,
            hist
        );
    }
}
