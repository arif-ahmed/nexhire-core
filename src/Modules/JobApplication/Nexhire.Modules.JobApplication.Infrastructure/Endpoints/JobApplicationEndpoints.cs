using System;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using Nexhire.Modules.JobApplication.Core.JobApplications.Commands;
using Nexhire.Modules.JobApplication.Core.JobApplications.Queries;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Infrastructure.Endpoints;

public static class JobApplicationEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var bookmarksGroup = app.MapGroup("api/bookmarks")
            .WithTags("Bookmarks");

        var applicationsGroup = app.MapGroup("api/applications")
            .WithTags("Applications");

        // 1. Add Bookmark
        bookmarksGroup.MapPost("", async ([FromBody] AddBookmarkRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var seekerId = GetUserId(principal);
            if (seekerId == null) return Results.Unauthorized();

            var command = new AddBookmarkCommand(seekerId.Value, request.JobPostingId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/bookmarks/{request.JobPostingId}", result.Value)
                : HandleFailure(result.Error);
        })
        .WithName("AddBookmark")
        .WithSummary("Bookmarks a job posting for the authenticated seeker")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 2. Remove Bookmark
        bookmarksGroup.MapDelete("{postingId:guid}", async (Guid postingId, ClaimsPrincipal principal, ISender sender) =>
        {
            var seekerId = GetUserId(principal);
            if (seekerId == null) return Results.Unauthorized();

            var command = new RemoveBookmarkCommand(seekerId.Value, postingId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : HandleFailure(result.Error);
        })
        .WithName("RemoveBookmark")
        .WithSummary("Removes a bookmarked job posting for the authenticated seeker")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 3. Get My Bookmarks
        bookmarksGroup.MapGet("", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var seekerId = GetUserId(principal);
            if (seekerId == null) return Results.Unauthorized();

            var query = new GetMyBookmarksQuery(seekerId.Value);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : HandleFailure(result.Error);
        })
        .WithName("GetMyBookmarks")
        .WithSummary("Retrieves all bookmarked job postings for the authenticated seeker")
        .Produces<IReadOnlyCollection<BookmarkedJobDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 4. Submit Application (Idempotency Enforced)
        applicationsGroup.MapPost("", async (
            HttpContext httpContext,
            [FromBody] SubmitApplicationRequest request,
            ClaimsPrincipal principal,
            ISender sender,
            IIdempotencyKeyStore idempotencyKeyStore) =>
        {
            var seekerId = GetUserId(principal);
            if (seekerId == null) return Results.Unauthorized();

            // Enforce client-supplied Idempotency-Key header
            if (!httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var headerValues) ||
                !Guid.TryParse(headerValues.ToString(), out var idempotencyKey))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Idempotency Key Required",
                    detail: "A valid 'Idempotency-Key' header in GUID format is required in the request headers."
                );
            }

            // Check if this key was already successfully processed
            var existingApplicationId = await idempotencyKeyStore.TryGetAsync(idempotencyKey, httpContext.RequestAborted);

            var command = new SubmitApplicationCommand(
                seekerId.Value,
                request.JobPostingId,
                request.ResumeDocumentId,
                request.CoverLetter,
                request.Overrides,
                idempotencyKey
            );

            var result = await sender.Send(command);

            if (!result.IsSuccess)
            {
                return HandleFailure(result.Error);
            }

            if (existingApplicationId.HasValue)
            {
                // Return 200 OK for an already existing resource (idempotent duplicate submission)
                return Results.Ok(result.Value);
            }

            // Return 201 Created for a newly created application.
            return Results.Created($"/api/applications/{result.Value.ApplicationId}", result.Value);
        })
        .WithName("SubmitApplication")
        .WithSummary("Submits a job application for the authenticated seeker")
        .Produces<SubmitApplicationResponse>(StatusCodes.Status201Created)
        .Produces<SubmitApplicationResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 5. Withdraw Application
        applicationsGroup.MapPost("{applicationId:guid}/withdraw", async (
            Guid applicationId,
            [FromBody] WithdrawRequest request,
            ClaimsPrincipal principal,
            ISender sender) =>
        {
            var seekerId = GetUserId(principal);
            if (seekerId == null) return Results.Unauthorized();

            var command = new WithdrawApplicationCommand(
                applicationId,
                seekerId.Value,
                request.ReasonCode,
                request.Comment
            );

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : HandleFailure(result.Error);
        })
        .WithName("WithdrawApplication")
        .WithSummary("Withdraws a job application for the authenticated seeker")
        .Produces<WithdrawApplicationResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 6. Get My Applications
        applicationsGroup.MapGet("", async (
            [FromQuery] string? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ClaimsPrincipal principal,
            ISender sender) =>
        {
            var seekerId = GetUserId(principal);
            if (seekerId == null) return Results.Unauthorized();

            var query = new GetMyApplicationsQuery(
                seekerId.Value,
                status,
                page ?? 1,
                pageSize ?? 10
            );

            var result = await sender.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : HandleFailure(result.Error);
        })
        .WithName("GetMyApplications")
        .WithSummary("Retrieves a paged list of job applications for the authenticated seeker")
        .Produces<PagedResult<ApplicationListItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 7. Get My Application Detail
        applicationsGroup.MapGet("{applicationId:guid}", async (
            Guid applicationId,
            ClaimsPrincipal principal,
            ISender sender) =>
        {
            var seekerId = GetUserId(principal);
            if (seekerId == null) return Results.Unauthorized();

            var query = new GetMyApplicationDetailQuery(applicationId, seekerId.Value);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : HandleFailure(result.Error);
        })
        .WithName("GetMyApplicationDetail")
        .WithSummary("Retrieves detailed information for a specific job application")
        .Produces<ApplicationDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal?.FindFirst(ClaimTypes.NameIdentifier) ?? principal?.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }

    private static IResult HandleFailure(Error error)
    {
        var statusCode = error.Code switch
        {
            "E-APP-FORBIDDEN" => StatusCodes.Status403Forbidden,
            "E-APP-NOT-FOUND" => StatusCodes.Status404NotFound,
            "E-APP-DUPLICATE" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            statusCode: statusCode,
            title: GetErrorTitle(error.Code),
            detail: error.Message,
            extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code }
        );
    }

    private static string GetErrorTitle(string errorCode) => errorCode switch
    {
        "E-APP-FORBIDDEN" => "Access Forbidden",
        "E-APP-NOT-FOUND" => "Resource Not Found",
        "E-APP-DUPLICATE" => "Duplicate Conflict",
        "E-APP-PROFILE-INCOMPLETE" => "Profile Level 2 Incomplete",
        "E-APP-INVALID-TRANSITION" => "Invalid State Transition",
        "E-APP-POSTING-CLOSED" => "Job Posting Closed",
        _ => "Business Rule Violation"
    };
}

// Minimal API payload types
public record AddBookmarkRequest(Guid JobPostingId);
public record SubmitApplicationRequest(
    Guid JobPostingId,
    Guid ResumeDocumentId,
    string? CoverLetter,
    SnapshotOverrides? Overrides
);
public record WithdrawRequest(string ReasonCode, string? Comment);
