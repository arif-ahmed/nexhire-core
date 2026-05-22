using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.AddFavoriteJob;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.CreateSavedSearch;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.DeleteSavedSearch;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.DismissRecommendation;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RememberSearchCriteria;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RemoveFavoriteJob;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RenameSavedSearch;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.SetSavedSearchNotification;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.UpdateSavedSearchCriteria;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.GetFavorites;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.GetSavedSearches;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.GetSearchSession;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.RefineSearchResults;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.RunSavedSearch;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.SearchJobs;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Endpoints;

public static class SearchDiscoveryEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/search")
            .WithTags("Search & Discovery");

        // Search (anonymous allowed)
        group.MapPost("jobs", async (SearchJobsRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SearchJobsQuery(request.Keyword, request.FiltersJson, request.SeekerUserId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("SearchJobs")
        .WithSummary("Search for jobs by keyword and filters");

        // Refine search
        group.MapPost("jobs/refine", async (RefineSearchRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RefineSearchResultsQuery(request.SeekerUserId, request.FiltersJson));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("RefineSearchResults")
        .WithSummary("Refine previous search results with additional filters");

        // Session
        group.MapGet("session", async (Guid seekerUserId, ISender sender) =>
        {
            var result = await sender.Send(new GetSearchSessionQuery(seekerUserId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("GetSearchSession")
        .WithSummary("Get current search session state");

        // Dismiss recommendation
        group.MapPost("recommendations/{postingId:guid}/dismiss", async (Guid postingId, Guid seekerUserId, ISender sender) =>
        {
            var result = await sender.Send(new DismissRecommendationCommand(seekerUserId, postingId));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("DismissRecommendation")
        .WithSummary("Dismiss a recommendation for current session");

        // Favorites
        group.MapGet("favorites", async (Guid seekerUserId, ISender sender) =>
        {
            var result = await sender.Send(new GetFavoritesQuery(seekerUserId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetFavorites")
        .WithSummary("List favorited jobs");

        group.MapPost("favorites", async (AddFavoriteRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AddFavoriteJobCommand(request.SeekerUserId, request.PostingId));
            return result.IsSuccess ? Results.Created($"/api/search/favorites/{result.Value}", result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("AddFavoriteJob")
        .WithSummary("Add a job to favorites");

        group.MapDelete("favorites/{postingId:guid}", async (Guid postingId, Guid seekerUserId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveFavoriteJobCommand(seekerUserId, postingId));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("RemoveFavoriteJob")
        .WithSummary("Remove a job from favorites");

        // Saved Searches
        group.MapGet("saved-searches", async (Guid seekerUserId, ISender sender) =>
        {
            var result = await sender.Send(new GetSavedSearchesQuery(seekerUserId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetSavedSearches")
        .WithSummary("List saved searches");

        group.MapPost("saved-searches", async (CreateSavedSearchRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateSavedSearchCommand(
                request.SeekerUserId, request.Name, request.Keyword, request.FiltersJson, request.NotificationPreference));
            return result.IsSuccess ? Results.Created($"/api/search/saved-searches/{result.Value}", result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("CreateSavedSearch")
        .WithSummary("Create a new saved search");

        group.MapPut("saved-searches/{id:guid}/name", async (Guid id, RenameSavedSearchRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RenameSavedSearchCommand(id, request.SeekerUserId, request.NewName));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("RenameSavedSearch")
        .WithSummary("Rename a saved search");

        group.MapPut("saved-searches/{id:guid}/criteria", async (Guid id, UpdateSavedSearchCriteriaRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateSavedSearchCriteriaCommand(id, request.SeekerUserId, request.Keyword, request.FiltersJson));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("UpdateSavedSearchCriteria")
        .WithSummary("Update saved search criteria");

        group.MapPut("saved-searches/{id:guid}/notification", async (Guid id, SetNotificationRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SetSavedSearchNotificationCommand(id, request.SeekerUserId, request.Preference));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("SetSavedSearchNotification")
        .WithSummary("Set notification preference for a saved search");

        group.MapDelete("saved-searches/{id:guid}", async (Guid id, Guid seekerUserId, ISender sender) =>
        {
            var result = await sender.Send(new DeleteSavedSearchCommand(id, seekerUserId));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .WithName("DeleteSavedSearch")
        .WithSummary("Delete a saved search");

        group.MapPost("saved-searches/{id:guid}/run", async (Guid id, Guid seekerUserId, ISender sender) =>
        {
            var result = await sender.Send(new RunSavedSearchQuery(id, seekerUserId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("RunSavedSearch")
        .WithSummary("Execute a saved search");
    }
}

// Request DTOs
public record SearchJobsRequest(string? Keyword, string? FiltersJson, Guid? SeekerUserId);
public record RefineSearchRequest(Guid SeekerUserId, string? FiltersJson);
public record AddFavoriteRequest(Guid SeekerUserId, Guid PostingId);
public record CreateSavedSearchRequest(Guid SeekerUserId, string Name, string? Keyword, string? FiltersJson, NotificationPreference NotificationPreference);
public record RenameSavedSearchRequest(Guid SeekerUserId, string NewName);
public record UpdateSavedSearchCriteriaRequest(Guid SeekerUserId, string? Keyword, string? FiltersJson);
public record SetNotificationRequest(Guid SeekerUserId, NotificationPreference Preference);
