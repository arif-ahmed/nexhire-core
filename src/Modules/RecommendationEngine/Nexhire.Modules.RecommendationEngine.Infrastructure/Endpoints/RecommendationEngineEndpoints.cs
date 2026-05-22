using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Modules.RecommendationEngine.Core.RecommendationEngine.Commands;
using Nexhire.Modules.RecommendationEngine.Core.RecommendationEngine.Queries;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Endpoints;

public static class RecommendationEngineEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/recommendations")
            .WithTags("Recommendation Engine");

        // ── Seeker Routes ──

        group.MapGet("jobs", async (Guid jobSeekerId, int? limit, ISender sender) =>
        {
            var result = await sender.Send(new GetJobRecommendationsQuery(jobSeekerId, limit ?? 10));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetJobRecommendations")
        .WithSummary("Get personalized job recommendations for a seeker");

        group.MapGet("jobs/{postingId:guid}/match", async (Guid postingId, Guid jobSeekerId, ISender sender) =>
        {
            var result = await sender.Send(new GetMatchDetailsQuery(jobSeekerId, postingId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetMatchDetails")
        .WithSummary("Get detailed match breakdown for a seeker-posting pair");

        group.MapPost("jobs/{postingId:guid}/feedback", async (Guid postingId, RecordFeedbackRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RecordRecommendationFeedbackCommand(request.JobSeekerId, postingId, request.Signal));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("RecordRecommendationFeedback")
        .WithSummary("Record seeker feedback on a recommended job");

        // ── Recruiter: Candidate Shortlist Routes ──

        group.MapGet("postings/{postingId:guid}/candidates", async (Guid postingId, Guid recruiterId, ISender sender) =>
        {
            var result = await sender.Send(new GetCandidateShortlistQuery(postingId, recruiterId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetCandidateShortlist")
        .WithSummary("Get ranked candidate shortlist for a posting");

        group.MapPost("postings/{postingId:guid}/candidates/refresh", async (Guid postingId, Guid recruiterId, ISender sender) =>
        {
            var result = await sender.Send(new RefreshCandidateShortlistCommand(postingId, recruiterId));
            return result.IsSuccess ? Results.Accepted() : Results.BadRequest(result.Error);
        })
        .WithName("RefreshCandidateShortlist")
        .WithSummary("Trigger candidate shortlist refresh for a posting");

        group.MapGet("postings/{postingId:guid}/candidates/{seekerId:guid}/fit", async (Guid postingId, Guid seekerId, ISender sender) =>
        {
            var result = await sender.Send(new GetCandidateFitAnalysisQuery(postingId, seekerId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetCandidateFitAnalysis")
        .WithSummary("Get fit analysis for a candidate-posting pair");

        group.MapPut("postings/{postingId:guid}/qualification-threshold", async (Guid postingId, SetQualificationThresholdRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SetQualificationThresholdCommand(
                postingId, request.RecruiterId, request.MinOverallMatch, request.MinSkillMatch,
                request.MinEducationLevel, request.MinExperienceYears, request.RequiredCertifications));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("SetQualificationThreshold")
        .WithSummary("Set qualification threshold for a posting's shortlist");

        group.MapPut("postings/{postingId:guid}/shortlist-size", async (Guid postingId, SetShortlistSizeRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SetShortlistSizeCommand(postingId, request.Size));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("SetShortlistSize")
        .WithSummary("Set the configured shortlist size for a posting");

        // ── Candidate Search ──

        group.MapPost("candidates/search", async (SearchCandidatesRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SearchCandidatesQuery(
                request.Keyword, request.Skills, request.EducationLevel,
                request.MinExperience, request.MaxSalary,
                request.Page ?? 1, request.PageSize ?? 10));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("SearchCandidates")
        .WithSummary("Search candidate database with faceted filters");

        // ── Config: Match Threshold ──

        group.MapGet("config/threshold", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMatchThresholdConfigQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetMatchThresholdConfig")
        .WithSummary("Get current match threshold configuration");

        group.MapPut("config/threshold", async (UpdateThresholdRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateMatchThresholdCommand(request.NewThreshold, request.AdminId));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateMatchThreshold")
        .WithSummary("Update global match threshold");

        group.MapPut("config/threshold/postings/{postingId:guid}", async (Guid postingId, SetPerPostingThresholdRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SetPerPostingThresholdCommand(postingId, request.Percent, request.ChangedBy));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("SetPerPostingThreshold")
        .WithSummary("Set per-posting threshold override");

        group.MapPost("config/threshold/preview", async (PreviewThresholdRequest request, ISender sender) =>
        {
            var result = await sender.Send(new PreviewThresholdImpactQuery(request.JobPostingId, request.ProposedThreshold));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("PreviewThresholdImpact")
        .WithSummary("Preview impact of threshold change");

        // ── Config: Weight Profiles ──

        group.MapGet("config/weights", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMatchingWeightsQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetMatchingWeights")
        .WithSummary("Get active matching weight profiles");

        group.MapGet("config/weights/history", async (int? take, ISender sender) =>
        {
            var result = await sender.Send(new GetWeightProfileHistoryQuery(take ?? 10));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetWeightProfileHistory")
        .WithSummary("Get weight profile version history");

        group.MapPost("config/weights/variants", async (CreateWeightVariantRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateWeightVariantCommand(
                request.Version, request.Skill, request.Education, request.Training,
                request.Location, request.Experience, request.Salary,
                request.VariantId, request.AllocationPercent, request.CreatedBy));
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        })
        .WithName("CreateWeightVariant")
        .WithSummary("Create a new weight profile variant");

        group.MapPut("config/weights", async (UpdateWeightsRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateMatchingWeightsCommand(
                request.Skill, request.Education, request.Training,
                request.Location, request.Experience, request.Salary, request.AdminId));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateMatchingWeights")
        .WithSummary("Update active matching weight profile");

        group.MapPost("config/weights/{version}/activate", async (string version, ISender sender) =>
        {
            var result = await sender.Send(new ActivateWeightProfileCommand(version));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("ActivateWeightProfile")
        .WithSummary("Activate a specific weight profile version");

        group.MapPost("config/weights/rollback", async (RollbackWeightsRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RollbackWeightProfileCommand(request.TargetVersion));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("RollbackWeightProfile")
        .WithSummary("Rollback to a previous weight profile version");

        // ── Talent Pools ──

        group.MapGet("talent-pools", async (Guid recruiterId, Guid employerId, ISender sender) =>
        {
            var result = await sender.Send(new GetTalentPoolsQuery(recruiterId, employerId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetTalentPools")
        .WithSummary("List talent pools for a recruiter/employer");

        group.MapPost("talent-pools", async (CreateTalentPoolRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateTalentPoolCommand(
                request.EmployerId, request.RecruiterId, request.Name,
                request.Description, request.Tags, request.IsShared));
            return result.IsSuccess ? Results.Created($"/api/recommendations/talent-pools/{result.Value}", result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("CreateTalentPool")
        .WithSummary("Create a new talent pool");

        group.MapGet("talent-pools/{poolId:guid}", async (Guid poolId, ISender sender) =>
        {
            var result = await sender.Send(new GetTalentPoolQuery(poolId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetTalentPool")
        .WithSummary("Get talent pool details with candidates");

        group.MapPut("talent-pools/{poolId:guid}", async (Guid poolId, UpdateTalentPoolRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateTalentPoolCommand(
                poolId, request.Name, request.Description, request.AssociatedSkills, request.IsShared));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateTalentPool")
        .WithSummary("Update talent pool metadata");

        group.MapPost("talent-pools/{poolId:guid}/candidates", async (Guid poolId, AddCandidateRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AddCandidateToTalentPoolCommand(poolId, request.JobSeekerId, request.RecruiterId, request.Note));
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        })
        .WithName("AddCandidateToTalentPool")
        .WithSummary("Add a candidate to a talent pool");

        group.MapPut("talent-pools/{poolId:guid}/candidates/{seekerId:guid}/note", async (Guid poolId, Guid seekerId, UpdateCandidateNoteRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateTalentPoolCandidateNoteCommand(poolId, seekerId, request.Note));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateTalentPoolCandidateNote")
        .WithSummary("Update a candidate's note in a talent pool");

        group.MapDelete("talent-pools/{poolId:guid}/candidates/{seekerId:guid}", async (Guid poolId, Guid seekerId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveCandidateFromTalentPoolCommand(poolId, seekerId));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("RemoveCandidateFromTalentPool")
        .WithSummary("Remove a candidate from a talent pool (soft-delete)");
    }
}

// ── Request DTOs ──

public record RecordFeedbackRequest(Guid JobSeekerId, FeedbackSignal Signal);
public record SetQualificationThresholdRequest(Guid RecruiterId, int MinOverallMatch, int MinSkillMatch, EducationLevel MinEducationLevel, decimal MinExperienceYears, List<string> RequiredCertifications);
public record SetShortlistSizeRequest(int Size);
public record SearchCandidatesRequest(string? Keyword, string? Skills, string? EducationLevel, decimal? MinExperience, decimal? MaxSalary, int? Page, int? PageSize);
public record UpdateThresholdRequest(int NewThreshold, Guid AdminId);
public record SetPerPostingThresholdRequest(int? Percent, Guid ChangedBy);
public record PreviewThresholdRequest(Guid JobPostingId, int ProposedThreshold);
public record CreateWeightVariantRequest(string Version, decimal Skill, decimal Education, decimal Training, decimal Location, decimal Experience, decimal Salary, string VariantId, int AllocationPercent, Guid CreatedBy);
public record UpdateWeightsRequest(decimal Skill, decimal Education, decimal Training, decimal Location, decimal Experience, decimal Salary, Guid AdminId);
public record RollbackWeightsRequest(string TargetVersion);
public record CreateTalentPoolRequest(Guid EmployerId, Guid RecruiterId, string Name, string? Description, List<string>? Tags, bool IsShared);
public record UpdateTalentPoolRequest(string? Name, string? Description, List<string>? AssociatedSkills, bool? IsShared);
public record AddCandidateRequest(Guid JobSeekerId, Guid RecruiterId, string? Note);
public record UpdateCandidateNoteRequest(string? Note);
