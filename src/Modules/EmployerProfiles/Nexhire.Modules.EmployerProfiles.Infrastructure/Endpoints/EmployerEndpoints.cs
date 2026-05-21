using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.AddCandidateToShortlist;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.ApproveEmployerVerification;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.CompleteEmployerLevel2;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.CreateShortlist;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.DeleteShortlist;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RegisterEmployer;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RejectEmployerVerification;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RemoveCandidateFromShortlist;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RemoveCompanyImage;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RemoveEmployerDocument;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RenameShortlist;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RequestEmployerVerification;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.ResubmitEmployerVerification;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UploadCompanyImage;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UploadEmployerDocument;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UploadEmployerLogo;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetEmployerDashboard;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetEmployerVerificationStatus;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetMatchedCandidates;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetMyEmployerProfile;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetPublicEmployerProfile;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetShortlist;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetShortlists;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Endpoints;

public static class EmployerEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/employers")
            .WithTags("Employers");

        // 1. Anonymous registration
        group.MapPost("", async (RegisterEmployerCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Created($"/api/employers/me", result.Value) 
                : Results.BadRequest(result.Error);
        })
        .WithName("RegisterEmployer")
        .WithSummary("Registers a new employer account");

        // 2. Authenticated Profile & Status
        group.MapGet("me", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var result = await sender.Send(new GetMyEmployerProfileQuery(userId.Value));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetMyEmployerProfile")
        .WithSummary("Retrieves the authenticated employer's profile");

        group.MapPut("me/level2", async (CompleteEmployerLevel2Request request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new CompleteEmployerLevel2Command(
                userId.Value,
                request.Website,
                request.Industry,
                request.CompanySize,
                request.Address,
                request.Description);

            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("CompleteEmployerLevel2")
        .WithSummary("Completes Level 2 profile details for the authenticated employer");

        group.MapPost("me/verification", async (RequestEmployerVerificationRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new RequestEmployerVerificationCommand(userId.Value, request.RegistryRef);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RequestEmployerVerification")
        .WithSummary("Requests verification for the authenticated employer");

        group.MapPost("me/resubmit-verification", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new ResubmitEmployerVerificationCommand(userId.Value);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("ResubmitEmployerVerification")
        .WithSummary("Resubmits verification for the authenticated employer");

        group.MapGet("me/verification-status", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var result = await sender.Send(new GetEmployerVerificationStatusQuery(userId.Value));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetEmployerVerificationStatus")
        .WithSummary("Retrieves the verification status for the authenticated employer");

        group.MapGet("me/dashboard", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var result = await sender.Send(new GetEmployerDashboardQuery(userId.Value));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetEmployerDashboard")
        .WithSummary("Retrieves the dashboard metrics for the authenticated employer");

        group.MapGet("me/matched-candidates", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var result = await sender.Send(new GetMatchedCandidatesQuery(userId.Value));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetMatchedCandidates")
        .WithSummary("Retrieves matched candidates for the authenticated employer");

        // 3. Media & Uploads
        group.MapPut("me/logo", async (IFormFile file, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            if (file == null || file.Length == 0) return Results.BadRequest("File is required.");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var content = stream.ToArray();

            var command = new UploadEmployerLogoCommand(userId.Value, content, file.FileName, file.ContentType);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("UploadEmployerLogo")
        .WithSummary("Uploads a company logo for the authenticated employer")
        .DisableAntiforgery();

        group.MapPost("me/images", async (IFormFile file, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            if (file == null || file.Length == 0) return Results.BadRequest("File is required.");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var content = stream.ToArray();

            var command = new UploadCompanyImageCommand(userId.Value, content, file.FileName, file.ContentType);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("UploadCompanyImage")
        .WithSummary("Uploads a company gallery image for the authenticated employer")
        .DisableAntiforgery();

        group.MapDelete("me/images/{imageId:guid}", async (Guid imageId, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new RemoveCompanyImageCommand(userId.Value, imageId);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RemoveCompanyImage")
        .WithSummary("Removes a company gallery image for the authenticated employer");

        group.MapPost("me/documents", async (IFormFile file, [FromQuery] DocumentKind kind, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            if (file == null || file.Length == 0) return Results.BadRequest("File is required.");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var content = stream.ToArray();

            var command = new UploadEmployerDocumentCommand(userId.Value, content, file.FileName, file.ContentType, kind);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("UploadEmployerDocument")
        .WithSummary("Uploads a supplementary document for the authenticated employer")
        .DisableAntiforgery();

        group.MapDelete("me/documents/{documentId:guid}", async (Guid documentId, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new RemoveEmployerDocumentCommand(userId.Value, documentId);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RemoveEmployerDocument")
        .WithSummary("Removes a supplementary document for the authenticated employer");

        // 4. Shortlists
        group.MapGet("me/shortlists", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var result = await sender.Send(new GetShortlistsQuery(userId.Value));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetShortlists")
        .WithSummary("Retrieves all shortlists for the authenticated employer");

        group.MapPost("me/shortlists", async (CreateShortlistRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new CreateShortlistCommand(userId.Value, request.Name);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Created($"/api/employers/me/shortlists/{result.Value}", result.Value) 
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateShortlist")
        .WithSummary("Creates a new shortlist for the authenticated employer");

        group.MapGet("me/shortlists/{shortlistId:guid}", async (Guid shortlistId, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var result = await sender.Send(new GetShortlistQuery(userId.Value, shortlistId));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetShortlistById")
        .WithSummary("Retrieves a shortlist by ID for the authenticated employer");

        group.MapPut("me/shortlists/{shortlistId:guid}", async (Guid shortlistId, RenameShortlistRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new RenameShortlistCommand(userId.Value, shortlistId, request.NewName);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RenameShortlist")
        .WithSummary("Renames a shortlist for the authenticated employer");

        group.MapDelete("me/shortlists/{shortlistId:guid}", async (Guid shortlistId, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new DeleteShortlistCommand(userId.Value, shortlistId);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("DeleteShortlist")
        .WithSummary("Deletes a shortlist for the authenticated employer");

        group.MapPost("me/shortlists/{shortlistId:guid}/candidates", async (Guid shortlistId, AddCandidateToShortlistRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new AddCandidateToShortlistCommand(userId.Value, shortlistId, request.CandidateUserId, request.MatchScore);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("AddCandidateToShortlist")
        .WithSummary("Adds a candidate to a shortlist for the authenticated employer");

        group.MapDelete("me/shortlists/{shortlistId:guid}/candidates/{memberId:guid}", async (Guid shortlistId, Guid memberId, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new RemoveCandidateFromShortlistCommand(userId.Value, shortlistId, memberId);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RemoveCandidateFromShortlist")
        .WithSummary("Removes a candidate from a shortlist for the authenticated employer");

        // 5. Public Profile
        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetPublicEmployerProfileQuery(id));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetPublicEmployerProfile")
        .WithSummary("Retrieves public details of an employer profile");

        // 6. Admin operations
        group.MapPost("{id:guid}/verify/approve", async (Guid id, ApproveEmployerVerificationRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var adminId = GetAdminId(principal);
            if (adminId == null) return Results.Unauthorized();

            var command = new ApproveEmployerVerificationCommand(id, adminId.Value, request.EvidenceRef);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("ApproveEmployerVerification")
        .WithSummary("Approves an employer profile verification (Admin)");

        group.MapPost("{id:guid}/verify/reject", async (Guid id, RejectEmployerVerificationRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var adminId = GetAdminId(principal);
            if (adminId == null) return Results.Unauthorized();

            var command = new RejectEmployerVerificationCommand(id, adminId.Value, request.Reason);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RejectEmployerVerification")
        .WithSummary("Rejects an employer profile verification (Admin)");
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal?.FindFirst(ClaimTypes.NameIdentifier) ?? principal?.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }

    private static Guid? GetAdminId(ClaimsPrincipal principal)
    {
        var claim = principal?.FindFirst(ClaimTypes.NameIdentifier) ?? principal?.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var adminId) ? adminId : null;
    }
}

// Request payloads
public record CompleteEmployerLevel2Request(
    string Website,
    string Industry,
    string CompanySize,
    AddressDto Address,
    string Description);

public record RequestEmployerVerificationRequest(string RegistryRef);

public record CreateShortlistRequest(string Name);

public record RenameShortlistRequest(string NewName);

public record AddCandidateToShortlistRequest(Guid CandidateUserId, int? MatchScore);

public record ApproveEmployerVerificationRequest(string EvidenceRef);

public record RejectEmployerVerificationRequest(string Reason);
