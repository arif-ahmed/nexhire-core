using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Nexhire.Modules.Notification.Application.CQRS.Commands;
using Nexhire.Modules.Notification.Application.CQRS.Queries;
using Nexhire.Modules.Notification.Domain;

namespace Nexhire.Modules.Notification.Infrastructure.Endpoints;

public static class NotificationEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications");

        // --- Notification Center ---
        group.MapGet("/", async (
            [FromHeader(Name = "X-User-Id")] Guid userId, // Mock auth mapping for modular routing testing
            [FromQuery] int pageSize,
            [FromQuery] int pageNumber,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new GetNotificationCenterQuery(userId, pageSize > 0 ? pageSize : 20, pageNumber > 0 ? pageNumber : 1));
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        });

        group.MapGet("/history", async (
            [FromHeader(Name = "X-User-Id")] Guid userId,
            [FromQuery] string? filterType,
            [FromQuery] string? searchTerm,
            [FromQuery] int skip,
            [FromQuery] int take,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new GetNotificationHistoryQuery(userId, filterType, searchTerm, skip, take > 0 ? take : 20));
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        });

        group.MapGet("/unread-count", async (
            [FromHeader(Name = "X-User-Id")] Guid userId,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new GetUnreadCountQuery(userId));
            return res.IsSuccess ? Results.Ok(new { count = res.Value }) : Results.BadRequest(res.Error);
        });

        group.MapPost("/{id}/read", async (
            Guid id,
            [FromHeader(Name = "X-User-Id")] Guid userId,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new MarkNotificationReadCommand(userId, new NotificationId(id)));
            return res.IsSuccess ? Results.NoContent() : Results.BadRequest(res.Error);
        });

        group.MapPost("/read-all", async (
            [FromHeader(Name = "X-User-Id")] Guid userId,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new MarkAllNotificationsReadCommand(userId));
            return res.IsSuccess ? Results.NoContent() : Results.BadRequest(res.Error);
        });

        group.MapDelete("/{id}", async (
            Guid id,
            [FromHeader(Name = "X-User-Id")] Guid userId,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new ArchiveNotificationCommand(userId, new NotificationId(id)));
            return res.IsSuccess ? Results.Ok(new { undoToken = res.Value }) : Results.BadRequest(res.Error);
        });

        group.MapDelete("/", async (
            [FromBody] List<Guid> ids,
            [FromHeader(Name = "X-User-Id")] Guid userId,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new ArchiveNotificationsBatchCommand(userId, ids));
            return res.IsSuccess ? Results.NoContent() : Results.BadRequest(res.Error);
        });

        group.MapPost("/{id}/undo-archive", async (
            Guid id,
            [FromQuery] string undoToken,
            [FromHeader(Name = "X-User-Id")] Guid userId,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new UndoArchiveNotificationCommand(userId, new NotificationId(id), undoToken));
            return res.IsSuccess ? Results.NoContent() : Results.BadRequest(res.Error);
        });

        // --- Preferences ---
        group.MapGet("/preferences", async (
            [FromHeader(Name = "X-User-Id")] Guid userId,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new GetMyNotificationPreferencesQuery(userId));
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        });

        group.MapPut("/preferences/email", async (
            [FromHeader(Name = "X-User-Id")] Guid userId,
            [FromBody] SetEmailPrefsRequest req,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new SetEmailNotificationPreferencesCommand(userId, req.EnabledToggles, req.FrequencyString, req.PreferredEmail));
            return res.IsSuccess ? Results.Ok() : Results.BadRequest(res.Error);
        });

        group.MapPut("/preferences/in-app", async (
            [FromHeader(Name = "X-User-Id")] Guid userId,
            [FromBody] SetInAppPrefsRequest req,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new SetInAppNotificationPreferencesCommand(userId, req.ToastModes, req.DndStart, req.DndEnd, req.IanaTz));
            return res.IsSuccess ? Results.Ok() : Results.BadRequest(res.Error);
        });

        group.MapPost("/preferences/email/opt-out", async (
            [FromHeader(Name = "X-User-Id")] Guid userId,
            [FromBody] SetOptOutRequest req,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new SetGlobalEmailOptOutCommand(userId, req.OptedOut, req.Method, req.IpAddress));
            return res.IsSuccess ? Results.Ok() : Results.BadRequest(res.Error);
        });

        // --- Phone Verification ---
        group.MapPost("/phone", async (
            [FromHeader(Name = "X-User-Id")] Guid userId,
            [FromBody] ProvidePhoneRequest req,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new ProvidePhoneNumberCommand(userId, req.PhoneNumber));
            return res.IsSuccess ? Results.Accepted() : Results.BadRequest(res.Error);
        });

        group.MapPost("/phone/confirm", async (
            [FromHeader(Name = "X-User-Id")] Guid userId,
            [FromBody] ConfirmPhoneRequest req,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new ConfirmPhoneNumberCommand(userId, req.Code));
            return res.IsSuccess ? Results.Ok() : Results.BadRequest(res.Error);
        });

        group.MapPut("/preferences/sms", async (
            [FromHeader(Name = "X-User-Id")] Guid userId,
            [FromBody] SetSmsOptRequest req,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new SetSmsOptInCommand(userId, req.OptIn, req.Method, req.IpAddress));
            return res.IsSuccess ? Results.Ok() : Results.BadRequest(res.Error);
        });

        // --- Admin Templates ---
        group.MapPost("/templates", async (
            [FromBody] CreateTemplateCommand cmd,
            IMediator mediator) =>
        {
            var res = await mediator.Send(cmd);
            return res.IsSuccess ? Results.Created($"/api/notifications/templates/{res.Value}", res.Value) : Results.BadRequest(res.Error);
        });

        group.MapPut("/templates/{id}/versions", async (
            Guid id,
            [FromBody] PublishTemplateVerRequest req,
            IMediator mediator) =>
        {
            var cmd = new PublishTemplateVersionCommand(id, req.Subject, req.BodyHtml, req.BodyText, req.Footer, req.Placeholders, req.CreatedByUserId);
            var res = await mediator.Send(cmd);
            return res.IsSuccess ? Results.Created($"/api/notifications/templates/{id}", res.Value) : Results.BadRequest(res.Error);
        });

        group.MapPost("/templates/{id}/rollback", async (
            Guid id,
            [FromBody] RollbackRequest req,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new RollbackTemplateCommand(id, req.VersionNumber, req.CreatedByUserId));
            return res.IsSuccess ? Results.Ok() : Results.BadRequest(res.Error);
        });

        group.MapPost("/templates/{id}/preview", async (
            Guid id,
            [FromBody] PreviewRequest req,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new PreviewTemplateQuery(id, req.VersionNumber, req.SamplePayload));
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        });

        group.MapGet("/templates", async (IMediator mediator) =>
        {
            var res = await mediator.Send(new ListTemplatesQuery());
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        });

        group.MapGet("/templates/{id}", async (Guid id, IMediator mediator) =>
        {
            var res = await mediator.Send(new GetTemplateQuery(id));
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        });

        // --- Administrative Log Audit ---
        group.MapGet("/admin/log", async (
            [FromQuery] Guid? userId,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] string? channel,
            [FromQuery] string? status,
            [FromQuery] int skip,
            [FromQuery] int take,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new GetDeliveryLogQuery(userId, fromUtc, toUtc, channel, status, skip, take > 0 ? take : 50));
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        });

        // --- Webhooks ---
        group.MapPost("/webhooks/email", async (
            [FromBody] EmailWebhookRequest req,
            IMediator mediator) =>
        {
            var res = await mediator.Send(new RecordDeliveryStatusCommand(req.ProviderMessageId, req.Status, req.Reason ?? ""));
            return res.IsSuccess ? Results.Ok() : Results.BadRequest(res.Error);
        });

        group.MapPost("/webhooks/sms", async (
            [FromBody] SmsWebhookRequest req,
            IMediator mediator) =>
        {
            // STOP keywords check
            if (req.BodyText != null && (req.BodyText.Trim().ToUpperInvariant() == "STOP" || req.BodyText.Trim().ToUpperInvariant() == "UNSUBSCRIBE"))
            {
                await mediator.Send(new HandleSmsStopKeywordCommand(req.FromNumber, req.BodyText));
            }

            var res = await mediator.Send(new RecordDeliveryStatusCommand(req.ProviderMessageId, req.Status, req.Reason ?? ""));
            return res.IsSuccess ? Results.Ok() : Results.BadRequest(res.Error);
        });

        // --- Anonymous Unsubscribe Route ---
        app.MapGet("/unsubscribe", async (
            [FromQuery] string token,
            IMediator mediator) =>
        {
            if (Guid.TryParse(token, out var userId))
            {
                var res = await mediator.Send(new SetGlobalEmailOptOutCommand(userId, true, "OneClickUnsubscribeLink", "127.0.0.1"));
                if (res.IsSuccess)
                {
                    return Results.Content("<h3>Unsubscribed Successfully</h3><p>You have been unsubscribed from all marketing emails.</p>", "text/html");
                }
            }
            return Results.BadRequest("Invalid unsubscribe token.");
        });
    }
}

// Request contracts
public record SetEmailPrefsRequest(Dictionary<string, bool> EnabledToggles, string FrequencyString, string? PreferredEmail);
public record SetInAppPrefsRequest(Dictionary<string, string> ToastModes, TimeOnly? DndStart, TimeOnly? DndEnd, string IanaTz);
public record SetOptOutRequest(bool OptedOut, string Method, string? IpAddress);
public record ProvidePhoneRequest(string PhoneNumber);
public record ConfirmPhoneRequest(string Code);
public record SetSmsOptRequest(bool OptIn, string Method, string? IpAddress);
public record PublishTemplateVerRequest(string? Subject, string BodyHtml, string BodyText, string Footer, List<string> Placeholders, Guid CreatedByUserId);
public record RollbackRequest(int VersionNumber, Guid CreatedByUserId);
public record PreviewRequest(int VersionNumber, Dictionary<string, string> SamplePayload);
public record EmailWebhookRequest(string ProviderMessageId, string Status, string? Reason);
public record SmsWebhookRequest(string FromNumber, string BodyText, string ProviderMessageId, string Status, string? Reason);
