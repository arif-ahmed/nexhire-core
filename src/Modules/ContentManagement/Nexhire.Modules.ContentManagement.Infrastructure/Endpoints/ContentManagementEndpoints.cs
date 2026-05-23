using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.ContentManagement.Core.Application.Commands;
using Nexhire.Modules.ContentManagement.Core.Application.DTOs;
using Nexhire.Modules.ContentManagement.Core.Application.Queries;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Infrastructure.Endpoints;

public static class ContentManagementEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/content").WithTags("Content Management");

        // --- Articles ---
        var articles = group.MapGroup("articles");

        articles.MapPost("", async (CreateArticleDraftCommand cmd, ISender sender) =>
        {
            var result = await sender.Send(cmd);
            return ToHttp(result, id => Results.Created($"/api/content/articles/{id}", id));
        });

        articles.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetArticleQuery(id));
            return ToHttp(result);
        });

        articles.MapPut("{id:guid}/content", async (Guid id, UpdateArticleContentCommand cmd, ISender sender) =>
        {
            var command = cmd with { ArticleId = id };
            return ToHttp(await sender.Send(command));
        });

        articles.MapDelete("{id:guid}/localizations/{lang}", async (Guid id, string lang, ISender sender) =>
            ToHttp(await sender.Send(new RemoveArticleLocalizationCommand(id, lang))));

        articles.MapPut("{id:guid}/category", async (Guid id, SetArticleCategoryCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { ArticleId = id })));

        articles.MapPut("{id:guid}/tags", async (Guid id, SetArticleTagsCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { ArticleId = id })));

        articles.MapPost("{id:guid}/media", async (Guid id, AddArticleMediaCommand cmd, ISender sender) =>
        {
            var result = await sender.Send(cmd with { ArticleId = id });
            return result.IsSuccess ? Results.StatusCode(201) : ToHttp(result);
        });

        articles.MapDelete("{id:guid}/media/{storageKey}", async (Guid id, string storageKey, ISender sender) =>
            ToHttp(await sender.Send(new RemoveArticleMediaCommand(id, storageKey))));

        articles.MapPost("{id:guid}/publish", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new PublishArticleCommand(id))));

        articles.MapPost("{id:guid}/schedule", async (Guid id, ScheduleArticleCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { ArticleId = id })));

        articles.MapDelete("{id:guid}/schedule", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new CancelArticleScheduleCommand(id))));

        articles.MapPut("{id:guid}/schedule", async (Guid id, RescheduleArticleCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { ArticleId = id })));

        articles.MapPost("{id:guid}/unpublish", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new UnpublishArticleCommand(id))));

        articles.MapPost("{id:guid}/archive", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new ArchiveArticleCommand(id))));

        articles.MapPost("archive-bulk", async (BulkArchiveArticlesCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd)));

        articles.MapPost("{id:guid}/restore", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new RestoreArticleFromArchiveCommand(id))));

        // --- News (anonymous) ---
        var news = group.MapGroup("news");

        news.MapGet("", async (string? categorySlug, string? tags, string language, int page, int pageSize, ISender sender) =>
        {
            var tagList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() as IReadOnlyList<string>;
            return ToHttp(await sender.Send(new BrowseNewsQuery(categorySlug, tagList, language, page, pageSize)));
        });

        news.MapGet("search", async (string query, string language, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, ISender sender) =>
            ToHttp(await sender.Send(new SearchNewsArchiveQuery(query, language, dateFrom, dateTo, page, pageSize))));

        news.MapGet("dashboard", async (Guid userId, int maxItems, ISender sender) =>
            ToHttp(await sender.Send(new GetDashboardNewsQuery(userId, maxItems))));

        // --- Categories ---
        var categories = group.MapGroup("categories");

        categories.MapGet("", async (ISender sender) =>
            ToHttp(await sender.Send(new GetCategoriesQuery())));

        categories.MapPost("", async (CreateCategoryCommand cmd, ISender sender) =>
        {
            var result = await sender.Send(cmd);
            return ToHttp(result, id => Results.Created($"/api/content/categories/{id}", id));
        });

        categories.MapPut("{id:guid}", async (Guid id, UpdateCategoryCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { CategoryId = id })));

        categories.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new DeleteCategoryCommand(id))));

        // --- FAQ ---
        var faq = group.MapGroup("faq");

        faq.MapPost("", async (CreateFaqEntryCommand cmd, ISender sender) =>
        {
            var result = await sender.Send(cmd);
            return ToHttp(result, id => Results.Created($"/api/content/faq/{id}", id));
        });

        faq.MapGet("{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetFaqEntryQuery(id))));

        faq.MapPut("{id:guid}/content", async (Guid id, UpdateFaqEntryContentCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { FaqEntryId = id })));

        faq.MapPut("{id:guid}/topics", async (Guid id, SetFaqTopicsCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { FaqEntryId = id })));

        faq.MapPut("{id:guid}/visible-roles", async (Guid id, SetFaqVisibleRolesCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { FaqEntryId = id })));

        faq.MapPut("{id:guid}/context-keys", async (Guid id, SetFaqContextKeysCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { FaqEntryId = id })));

        faq.MapPost("{id:guid}/multimedia", async (Guid id, AddFaqMultimediaBlockCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { FaqEntryId = id })));

        faq.MapPost("{id:guid}/publish", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new PublishFaqEntryCommand(id))));

        faq.MapPost("{id:guid}/unpublish", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new UnpublishFaqEntryCommand(id))));

        // --- Help Center (anonymous) ---
        var help = group.MapGroup("help");

        help.MapGet("", async (string? viewerRole, string language, ISender sender) =>
            ToHttp(await sender.Send(new BrowseHelpCenterQuery(viewerRole, language))));

        help.MapGet("search", async (string query, string? viewerRole, string language, ISender sender) =>
            ToHttp(await sender.Send(new SearchHelpContentQuery(query, viewerRole, language))));

        help.MapGet("context/{contextKey}", async (string contextKey, string? viewerRole, string language, ISender sender) =>
            ToHttp(await sender.Send(new GetContextHelpQuery(contextKey, viewerRole, language))));

        // --- Topics ---
        var topics = group.MapGroup("topics");

        topics.MapPost("", async (CreateTopicCommand cmd, ISender sender) =>
        {
            var result = await sender.Send(cmd);
            return ToHttp(result, id => Results.Created($"/api/content/topics/{id}", id));
        });

        topics.MapPut("{id:guid}", async (Guid id, UpdateTopicCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { TopicId = id })));

        topics.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new DeleteTopicCommand(id))));

        // --- Feedback (anonymous) ---
        group.MapPost("feedback", async (SubmitHelpFeedbackCommand cmd, ISender sender) =>
        {
            var result = await sender.Send(cmd);
            return ToHttp(result, () => Results.StatusCode(201));
        });

        group.MapGet("feedback/summary", async (ISender sender) =>
            ToHttp(await sender.Send(new GetFeedbackSummaryQuery())));

        // --- Guided Tours ---
        var tours = group.MapGroup("tours");

        tours.MapPost("", async (CreateGuidedTourCommand cmd, ISender sender) =>
        {
            var result = await sender.Send(cmd);
            return ToHttp(result, id => Results.Created($"/api/content/tours/{id}", id));
        });

        tours.MapPost("{tourId:guid}/steps", async (Guid tourId, AddTourStepCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { TourId = tourId })));

        tours.MapPut("{tourId:guid}/steps/{stepId:guid}", async (Guid tourId, Guid stepId, UpdateTourStepCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { TourId = tourId, StepId = stepId })));

        tours.MapDelete("{tourId:guid}/steps/{stepId:guid}", async (Guid tourId, Guid stepId, ISender sender) =>
            ToHttp(await sender.Send(new RemoveTourStepCommand(tourId, stepId))));

        tours.MapPut("{tourId:guid}/steps/reorder", async (Guid tourId, ReorderTourStepsCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { TourId = tourId })));

        tours.MapPut("{tourId:guid}/active", async (Guid tourId, SetTourActiveCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd with { TourId = tourId })));

        tours.MapGet("", async (string audience, string language, ISender sender) =>
            ToHttp(await sender.Send(new GetGuidedToursQuery(audience, language))));

        // --- Preferences ---
        group.MapPut("preferences", async (UpdateContentPreferenceCommand cmd, ISender sender) =>
            ToHttp(await sender.Send(cmd)));
    }

    private static IResult ToHttp(Result result) => result.IsSuccess
        ? Results.Ok()
        : result.Error.Code switch
        {
            "E-ARTICLE-NO-CATEGORY" or "E-ARTICLE-ILLEGAL-TRANSITION" or
            "E-CATEGORY-IN-USE" or "E-TOPIC-IN-USE" or
            "E-ARTICLE-NOT-FOUND" or "E-CATEGORY-NOT-FOUND" or
            "E-FAQ-NOT-FOUND" or "E-TOUR-NOT-FOUND" or "E-TOPIC-NOT-FOUND" or
            "E-PREFERENCE-NOT-FOUND" or "E-FAQ-NOT-HELP-ARTICLE"
                => Results.Conflict(result.Error),
            "E-SCHEDULE-PAST" or "E-BULK-LIMIT-EXCEEDED" or
            "E-INVALID-LANGUAGE" or "E-MEDIA-INVALID-KIND" or
            "E-FAQ-INVALID-KIND" or "E-CATEGORY-SLUG-TAKEN" or "E-TOPIC-SLUG-TAKEN"
                => Results.BadRequest(result.Error),
            "E-MEDIA-SIZE-EXCEEDED" => Results.StatusCode(413),
            _ => Results.BadRequest(result.Error)
        };

    private static IResult ToHttp<T>(Result<T> result, Func<T, IResult> onSuccess) => result.IsSuccess
        ? onSuccess(result.Value)
        : ToHttp(result);

    private static IResult ToHttp<T>(Result<T> result) => result.IsSuccess
        ? Results.Ok(result.Value)
        : result.Error.Code switch
        {
            "E-ARTICLE-NOT-FOUND" or "E-CATEGORY-NOT-FOUND" or
            "E-FAQ-NOT-FOUND" or "E-TOUR-NOT-FOUND" or "E-PREFERENCE-NOT-FOUND"
                => Results.NotFound(result.Error),
            _ => Results.BadRequest(result.Error)
        };

    private static IResult ToHttp(Result result, Func<IResult> onSuccess) => result.IsSuccess
        ? onSuccess()
        : ToHttp(result);
}
