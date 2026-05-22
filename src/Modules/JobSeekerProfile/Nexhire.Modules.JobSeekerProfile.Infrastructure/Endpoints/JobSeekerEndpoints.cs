using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddEducationEntry;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddExperienceEntry;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddSkill;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.ConfirmParsedResumeFields;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.DeleteSupplementaryDocument;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.DisablePublicSharing;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.EnablePublicSharing;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.MarkProfileSelfAttested;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RegeneratePublicSlug;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RegisterJobSeeker;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveEducationEntry;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveExperienceEntry;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveSkill;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RestoreProfileVersion;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetAddresses;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetJobPreferences;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetProfileVisibility;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetRecentSalary;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UpdateEducationEntry;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UpdateExperienceEntry;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UploadResume;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UploadSupplementaryDocument;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetEditHistory;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetMyProfile;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetProfileCompleteness;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetPublicProfile;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetResumeParseStatus;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Endpoints;

public static class JobSeekerEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/jobseekers")
            .WithTags("JobSeekers");

        // 1. Anonymous Seeker Registration
        group.MapPost("", async (RegisterJobSeekerCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Created($"/api/jobseekers/me", result.Value) 
                : Results.BadRequest(result.Error);
        })
        .WithName("RegisterJobSeeker")
        .WithSummary("Registers a new job seeker account");

        // 2. Retrieval of Seeker's Own Profile
        group.MapGet("me", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var result = await sender.Send(new GetMyProfileQuery(userId.Value));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetMyJobSeekerProfile")
        .WithSummary("Retrieves the authenticated job seeker's profile");

        // 3. Save/Update Job Preferences
        group.MapPut("me/preferences", async (SetJobPreferencesRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new SetJobPreferencesCommand(
                userId.Value,
                request.JobTypes,
                request.Industries,
                request.Locations,
                request.WorkArrangements,
                request.MinSalaryExpectation,
                request.MaxSalaryExpectation,
                request.SalaryCurrency);

            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("SetJobPreferences")
        .WithSummary("Updates job preferences for the authenticated job seeker");

        // 4. Save/Update Addresses
        group.MapPut("me/addresses", async (SetAddressesRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new SetAddressesCommand(
                userId.Value,
                request.CurrentAddress.Line1,
                request.CurrentAddress.Line2,
                request.CurrentAddress.City,
                request.CurrentAddress.District,
                request.CurrentAddress.Postcode,
                request.CurrentAddress.Country,
                request.PermanentAddress.Line1,
                request.PermanentAddress.Line2,
                request.PermanentAddress.City,
                request.PermanentAddress.District,
                request.PermanentAddress.Postcode,
                request.PermanentAddress.Country);

            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("SetAddresses")
        .WithSummary("Updates current and permanent addresses for the authenticated job seeker");

        // 5. Save/Update Recent Salary
        group.MapPut("me/salary", async (SetRecentSalaryRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new SetRecentSalaryCommand(userId.Value, request.Amount, request.Currency);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("SetRecentSalary")
        .WithSummary("Updates recent salary for the authenticated job seeker");

        // 6. Profile Visibility
        group.MapPut("me/visibility", async (SetProfileVisibilityRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new SetProfileVisibilityCommand(userId.Value, request.Visibility);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("SetProfileVisibility")
        .WithSummary("Updates visibility settings for the authenticated job seeker's profile");

        // 7. Add Education Entry
        group.MapPost("me/education", async (AddEducationEntryRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new AddEducationEntryCommand(
                userId.Value,
                request.Degree,
                request.Institution,
                request.StartDate,
                request.EndDate,
                request.Gpa);

            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("AddEducationEntry")
        .WithSummary("Adds an education entry to the job seeker's profile");

        // 8. Update Education Entry
        group.MapPut("me/education/{educationId:guid}", async (Guid educationId, UpdateEducationEntryRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new UpdateEducationEntryCommand(
                userId.Value,
                educationId,
                request.Degree,
                request.Institution,
                request.StartDate,
                request.EndDate,
                request.Gpa);

            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("UpdateEducationEntry")
        .WithSummary("Updates an education entry in the job seeker's profile");

        // 9. Remove Education Entry
        group.MapDelete("me/education/{educationId:guid}", async (Guid educationId, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new RemoveEducationEntryCommand(userId.Value, educationId);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RemoveEducationEntry")
        .WithSummary("Removes an education entry from the job seeker's profile");

        // 10. Add Experience Entry
        group.MapPost("me/experience", async (AddExperienceEntryRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new AddExperienceEntryCommand(
                userId.Value,
                request.Company,
                request.Role,
                request.StartDate,
                request.EndDate,
                request.IsCurrent,
                request.Responsibilities);

            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("AddExperienceEntry")
        .WithSummary("Adds a work experience entry to the job seeker's profile");

        // 11. Update Experience Entry
        group.MapPut("me/experience/{experienceId:guid}", async (Guid experienceId, UpdateExperienceEntryRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new UpdateExperienceEntryCommand(
                userId.Value,
                experienceId,
                request.Company,
                request.Role,
                request.StartDate,
                request.EndDate,
                request.IsCurrent,
                request.Responsibilities);

            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("UpdateExperienceEntry")
        .WithSummary("Updates a work experience entry in the job seeker's profile");

        // 12. Remove Experience Entry
        group.MapDelete("me/experience/{experienceId:guid}", async (Guid experienceId, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new RemoveExperienceEntryCommand(userId.Value, experienceId);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RemoveExperienceEntry")
        .WithSummary("Removes a work experience entry from the job seeker's profile");

        // 13. Add Skill
        group.MapPost("me/skills", async (AddSkillRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new AddSkillCommand(
                userId.Value,
                request.RawSkillLabel,
                request.Category,
                request.Tier,
                request.Proficiency);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("AddSkill")
        .WithSummary("Adds a skill to the job seeker's profile");

        // 14. Remove Skill
        group.MapDelete("me/skills/{skillId:guid}", async (Guid skillId, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new RemoveSkillCommand(userId.Value, skillId);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RemoveSkill")
        .WithSummary("Removes a skill from the job seeker's profile");

        // 15. Self Attestation
        group.MapPost("me/self-attest", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new MarkProfileSelfAttestedCommand(userId.Value);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("MarkProfileSelfAttested")
        .WithSummary("Marks the job seeker profile as self-attested");

        // 16. Enable Public Sharing
        group.MapPost("me/sharing/enable", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new EnablePublicSharingCommand(userId.Value);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("EnablePublicSharing")
        .WithSummary("Enables public profile sharing and configures custom URL slug");

        // 17. Disable Public Sharing
        group.MapPost("me/sharing/disable", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new DisablePublicSharingCommand(userId.Value);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("DisablePublicSharing")
        .WithSummary("Disables public profile sharing");

        // 18. Regenerate Public Slug
        group.MapPost("me/sharing/regenerate-slug", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new RegeneratePublicSlugCommand(userId.Value);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RegeneratePublicSlug")
        .WithSummary("Regenerates a new unique public URL slug for the profile");

        // 19. Retrieve Completeness Score details
        group.MapGet("me/completeness", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var result = await sender.Send(new GetProfileCompletenessQuery(userId.Value));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetMyProfileCompleteness")
        .WithSummary("Retrieves detailed profile completeness score and recommendations");

        // 20. Upload Resume
        group.MapPost("me/resume", async (IFormFile file, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var content = stream.ToArray();

            var command = new UploadResumeCommand(userId.Value, content, file.FileName, file.ContentType);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .DisableAntiforgery()
        .WithName("UploadResume")
        .WithSummary("Uploads, virus scans, and schedules parsing for the job seeker's resume");

        // 21. Get Resume Parse Status
        group.MapGet("me/resume/status", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var result = await sender.Send(new GetResumeParseStatusQuery(userId.Value));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetResumeParseStatus")
        .WithSummary("Retrieves the parsing status and parsed data of the active resume");

        // 22. Confirm Parsed Resume Fields
        group.MapPost("me/resume/confirm", async (ConfirmParsedResumeFieldsRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new ConfirmParsedResumeFieldsCommand(
                userId.Value,
                request.ResumeId,
                request.SelectedFieldKeys.ToList());

            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("ConfirmParsedResumeFields")
        .WithSummary("Merges confirmed parsed resume data fields into the job seeker's profile");

        // 23. Upload Supplementary Document
        group.MapPost("me/documents", async (IFormFile file, [FromQuery] string kind, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var content = stream.ToArray();

            var command = new UploadSupplementaryDocumentCommand(userId.Value, content, file.FileName, file.ContentType, kind);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .DisableAntiforgery()
        .WithName("UploadSupplementaryDocument")
        .WithSummary("Uploads, virus scans, and attaches a supplementary document (limit 10)");

        // 24. Delete Supplementary Document
        group.MapDelete("me/documents/{documentId:guid}", async (Guid documentId, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new DeleteSupplementaryDocumentCommand(userId.Value, documentId);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("DeleteSupplementaryDocument")
        .WithSummary("Deletes a supplementary document from the profile");

        // 25. Get Edit Snaphots History
        group.MapGet("me/history", async (ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var result = await sender.Send(new GetEditHistoryQuery(userId.Value));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetJobSeekerEditHistory")
        .WithSummary("Retrieves the audit/version snapshots history of the profile");

        // 26. Restore Profile Version Snapshot
        group.MapPost("me/history/restore/{versionId:guid}", async (Guid versionId, ClaimsPrincipal principal, ISender sender) =>
        {
            var userId = GetUserId(principal);
            if (userId == null) return Results.Unauthorized();

            var command = new RestoreProfileVersionCommand(userId.Value, versionId);
            var result = await sender.Send(command);

            return result.IsSuccess 
                ? Results.Ok() 
                : Results.BadRequest(result.Error);
        })
        .WithName("RestoreProfileVersion")
        .WithSummary("Restores the profile state from a historical snapshot version");

        // 27. Public Slug Access (Anonymous PII Secured)
        group.MapGet("public/{slug}", async (string slug, ISender sender) =>
        {
            var result = await sender.Send(new GetPublicProfileQuery(slug));

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetPublicJobSeekerProfile")
        .WithSummary("Retrieves a public job seeker profile by slug");
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal?.FindFirst(ClaimTypes.NameIdentifier) ?? principal?.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }
}

// Request Payload Types
public record SetJobPreferencesRequest(
    IEnumerable<string> JobTypes,
    IEnumerable<string> Industries,
    IEnumerable<string> Locations,
    IEnumerable<string> WorkArrangements,
    decimal? MinSalaryExpectation = null,
    decimal? MaxSalaryExpectation = null,
    string? SalaryCurrency = null);

public record AddressPayload(string Line1, string? Line2, string City, string District, string Postcode, string Country);
public record SetAddressesRequest(AddressPayload CurrentAddress, AddressPayload PermanentAddress);
public record SetRecentSalaryRequest(decimal Amount, string Currency);
public record SetProfileVisibilityRequest(string Visibility);

public record AddEducationEntryRequest(string Degree, string Institution, DateTime StartDate, DateTime? EndDate, decimal? Gpa);
public record UpdateEducationEntryRequest(string Degree, string Institution, DateTime StartDate, DateTime? EndDate, decimal? Gpa);

public record AddExperienceEntryRequest(string Company, string Role, DateTime StartDate, DateTime? EndDate, bool IsCurrent, string Responsibilities);
public record UpdateExperienceEntryRequest(string Company, string Role, DateTime StartDate, DateTime? EndDate, bool IsCurrent, string Responsibilities);

public record AddSkillRequest(string RawSkillLabel, string Category, string Tier, int Proficiency);

public record ConfirmParsedResumeFieldsRequest(
    Guid ResumeId,
    IEnumerable<string> SelectedFieldKeys);
