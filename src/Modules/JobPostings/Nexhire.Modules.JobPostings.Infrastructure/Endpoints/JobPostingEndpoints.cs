using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.JobPostings.Core.Domain.Ports;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Modules.JobPostings.Core.DTOs;
using Nexhire.Modules.JobPostings.Core.JobPostings.Commands;
using Nexhire.Modules.JobPostings.Core.JobPostings.Queries;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Infrastructure.Endpoints;

public static class JobPostingEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var employer = app.MapGroup("api/job-postings").WithTags("Job Postings");

        employer.MapPost("", async (JobPostingDraftDto draft, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            var employerId = GetEmployerId(principal) ?? userId;
            if (userId is null || employerId is null) return Results.Unauthorized();
            var result = await sender.Send(new CreateJobPostingCommand(employerId.Value, userId.Value, draft));
            return ToHttp(result, id => Results.Created($"/api/job-postings/{id}", id));
        });

        employer.MapGet("mine", async (ClaimsPrincipal principal, ISender sender, string? status) =>
        {
            var employerId = GetEmployerId(principal) ?? GetUserId(principal);
            if (employerId is null) return Results.Unauthorized();
            PostingStatus? parsedStatus = Enum.TryParse<PostingStatus>(status, true, out var value) ? value : null;
            var result = await sender.Send(new GetMyJobPostingsQuery(employerId.Value, parsedStatus));
            return ToHttp(result, Results.Ok);
        });

        employer.MapGet("{id:guid}", async (Guid id, ClaimsPrincipal principal, ISender sender) =>
        {
            var employerId = GetEmployerId(principal) ?? GetUserId(principal);
            if (employerId is null) return Results.Unauthorized();
            var result = await sender.Send(new GetJobPostingByIdQuery(id, employerId));
            return ToHttp(result, Results.Ok);
        });

        employer.MapGet("{id:guid}/schema-org", async (Guid id, ClaimsPrincipal principal, ISender sender) =>
        {
            var employerId = GetEmployerId(principal) ?? GetUserId(principal);
            if (employerId is null) return Results.Unauthorized();
            var result = await sender.Send(new GetSchemaOrgJobPostingQuery(id, employerId));
            return ToHttp(result, Results.Ok);
        });

        employer.MapPut("{id:guid}", async (Guid id, JobPostingDraftDto draft, ClaimsPrincipal principal, ISender sender) =>
        {
            var employerId = GetEmployerId(principal) ?? GetUserId(principal);
            if (employerId is null) return Results.Unauthorized();
            var result = await sender.Send(new UpdateJobPostingDetailsCommand(id, employerId.Value, draft));
            return ToHttp(result);
        });

        employer.MapPut("{id:guid}/deadline", async (Guid id, DeadlineRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var employerId = GetEmployerId(principal) ?? GetUserId(principal);
            if (employerId is null) return Results.Unauthorized();
            var result = await sender.Send(new ExtendApplicationDeadlineCommand(id, employerId.Value, request.NewDeadlineUtc, request.AutoCloseEnabled));
            return ToHttp(result);
        });

        employer.MapPut("{id:guid}/skills", async (Guid id, IReadOnlyCollection<SkillInput> request, ClaimsPrincipal principal, ISender sender) =>
        {
            var employerId = GetEmployerId(principal) ?? GetUserId(principal);
            if (employerId is null) return Results.Unauthorized();
            var result = await sender.Send(new UpdateRequiredSkillsCommand(id, employerId.Value, request));
            return ToHttp(result);
        });

        employer.MapPut("{id:guid}/visibility", async (Guid id, PostingVisibilityDto request, ClaimsPrincipal principal, ISender sender) =>
        {
            var employerId = GetEmployerId(principal) ?? GetUserId(principal);
            if (employerId is null) return Results.Unauthorized();
            var result = await sender.Send(new SetPostingVisibilityCommand(id, employerId.Value, request));
            return ToHttp(result);
        });

        employer.MapPost("{id:guid}/publish", async (Guid id, ClaimsPrincipal principal, ISender sender) =>
            await SendEmployerStatus(id, principal, sender, (postingId, employerId, userId) => new PublishJobPostingCommand(postingId, employerId, userId)));

        employer.MapPost("{id:guid}/pause", async (Guid id, ClaimsPrincipal principal, ISender sender) =>
            await SendEmployerStatus(id, principal, sender, (postingId, employerId, userId) => new PauseJobPostingCommand(postingId, employerId, userId)));

        employer.MapPost("{id:guid}/resume", async (Guid id, ClaimsPrincipal principal, ISender sender) =>
            await SendEmployerStatus(id, principal, sender, (postingId, employerId, userId) => new ResumeJobPostingCommand(postingId, employerId, userId)));

        employer.MapPost("{id:guid}/archive", async (Guid id, ClaimsPrincipal principal, ISender sender) =>
            await SendEmployerStatus(id, principal, sender, (postingId, employerId, userId) => new ArchiveJobPostingCommand(postingId, employerId, userId)));

        employer.MapPost("{id:guid}/renew", async (Guid id, DeadlineRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            var employerId = GetEmployerId(principal) ?? userId;
            if (userId is null || employerId is null) return Results.Unauthorized();
            var result = await sender.Send(new RenewJobPostingCommand(id, employerId.Value, userId.Value, request.NewDeadlineUtc, request.AutoCloseEnabled));
            return ToHttp(result, newId => Results.Created($"/api/job-postings/{newId}", newId));
        });

        employer.MapPost("renew-bulk", async (BulkRenewRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            var employerId = GetEmployerId(principal) ?? userId;
            if (userId is null || employerId is null) return Results.Unauthorized();
            var result = await sender.Send(new BulkRenewJobPostingsCommand(employerId.Value, userId.Value, request.JobPostingIds, request.NewDeadlineUtc, request.AutoCloseEnabled));
            return result.IsSuccess ? Results.Json(result.Value, statusCode: StatusCodes.Status207MultiStatus) : ToHttp(result);
        });

        employer.MapGet("{id:guid}/audit-trail", async (Guid id, ClaimsPrincipal principal, ISender sender) =>
        {
            var employerId = GetEmployerId(principal) ?? GetUserId(principal);
            if (employerId is null) return Results.Unauthorized();
            var result = await sender.Send(new GetPostingAuditTrailQuery(id, employerId));
            return ToHttp(result, Results.Ok);
        });

        employer.MapGet("{id:guid}/audit-trail/export", async (Guid id, string format, IAuditTrailExporter exporter) =>
        {
            var result = await exporter.ExportAsync(id, format, CancellationToken.None);
            return result.IsSuccess
                ? Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
                : ToHttp(result);
        });

        var admin = app.MapGroup("api/admin/job-postings").WithTags("Admin Job Postings");

        admin.MapGet("", async (ISender sender, Guid? employerId, string? status, DateTime? postedFrom, DateTime? postedTo, string? location, string? q) =>
        {
            PostingStatus? parsedStatus = Enum.TryParse<PostingStatus>(status, true, out var value) ? value : null;
            var result = await sender.Send(new AdminListJobPostingsQuery(employerId, parsedStatus, postedFrom, postedTo, location, q));
            return ToHttp(result, Results.Ok);
        });

        admin.MapGet("{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new AdminGetJobPostingDetailQuery(id)), Results.Ok));

        admin.MapPost("{id:guid}/suspend", async (Guid id, ModerationRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var adminId = GetUserId(principal);
            if (adminId is null) return Results.Unauthorized();
            return ToHttp(await sender.Send(new SuspendJobPostingCommand(id, adminId.Value, request.Reason)));
        });

        admin.MapPost("{id:guid}/reinstate", async (Guid id, ClaimsPrincipal principal, ISender sender) =>
        {
            var adminId = GetUserId(principal);
            if (adminId is null) return Results.Unauthorized();
            return ToHttp(await sender.Send(new ReinstateJobPostingCommand(id, adminId.Value)));
        });

        admin.MapPost("{id:guid}/remove", async (Guid id, ModerationRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var adminId = GetUserId(principal);
            if (adminId is null) return Results.Unauthorized();
            return ToHttp(await sender.Send(new RemoveJobPostingCommand(id, adminId.Value, request.Reason)));
        });
    }

    private static async Task<IResult> SendEmployerStatus(Guid id, ClaimsPrincipal principal, ISender sender, Func<Guid, Guid, Guid, IRequest<Result>> commandFactory)
    {
        var userId = GetUserId(principal);
        var employerId = GetEmployerId(principal) ?? userId;
        if (userId is null || employerId is null) return Results.Unauthorized();
        return ToHttp(await sender.Send(commandFactory(id, employerId.Value, userId.Value)));
    }

    private static IResult ToHttp(Result result)
    {
        if (result.IsSuccess) return Results.Ok();
        return result.Error.Code switch
        {
            "E-POST-NOT-FOUND" => Results.NotFound(result.Error),
            "E-POST-FORBIDDEN" => Results.Forbid(),
            "E-POST-EMPLOYER-NOT-ELIGIBLE" => Results.Json(result.Error, statusCode: StatusCodes.Status403Forbidden),
            "E-POST-NOT-SCHEMA-COMPLIANT" => Results.UnprocessableEntity(result.Error),
            _ when result.Error.Code.Contains("ILLEGAL") || result.Error.Code.Contains("NOT-LATER") || result.Error.Code.Contains("PAST") => Results.Conflict(result.Error),
            _ => Results.BadRequest(result.Error)
        };
    }

    private static IResult ToHttp<T>(Result<T> result, Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess) return onSuccess(result.Value);
        return ToHttp(Result.Failure(result.Error));
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    private static Guid? GetEmployerId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("employer_id") ?? principal.FindFirst("EmployerId");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }
}

public sealed record DeadlineRequest(DateTime NewDeadlineUtc, bool AutoCloseEnabled);
public sealed record ModerationRequest(string Reason);
public sealed record BulkRenewRequest(IReadOnlyCollection<Guid> JobPostingIds, DateTime NewDeadlineUtc, bool AutoCloseEnabled);
