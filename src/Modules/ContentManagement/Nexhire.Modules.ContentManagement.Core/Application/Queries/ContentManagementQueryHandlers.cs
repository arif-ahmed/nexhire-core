using Nexhire.Modules.ContentManagement.Core.Application.DTOs;
using Nexhire.Modules.ContentManagement.Core.Application.Ports;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.Repositories;
using Nexhire.Modules.ContentManagement.Core.Domain.Services;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Application.Queries;

public sealed class GetArticleQueryHandler(IArticleRepository repository) : IQueryHandler<GetArticleQuery, ArticleDto>
{
    public async Task<Result<ArticleDto>> Handle(GetArticleQuery request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure<ArticleDto>(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        return Result.Success(MapToDto(article));
    }

    private static ArticleDto MapToDto(Article a) => new(
        a.Id, a.AuthorUserId, a.Status.ToString(), a.PrimaryCategoryId, null,
        a.Localizations.ToDictionary(kv => kv.Key.ToString(), kv => new LocalizedContentDto(kv.Value.Title, kv.Value.Summary, kv.Value.BodyRichText)),
        a.Tags.Select(t => new ArticleTagDto(t.Language.ToString(), t.NormalizedLabel, t.DisplayLabel)).ToList(),
        a.Media.Select(m => new MediaRefDto(m.StorageKey, m.Url, m.MimeType, m.SizeBytes, m.Kind.ToString(), m.TranscriptUrl)).ToList(),
        a.PublicationSchedule?.PublishAtUtc.ToString("O"),
        a.PublishedOnUtc, a.CreatedOnUtc, a.UpdatedOnUtc);
}

public sealed class BrowseNewsQueryHandler(IArticleRepository repository) : IQueryHandler<BrowseNewsQuery, PagedResult<NewsFeedItemDto>>
{
    public async Task<Result<PagedResult<NewsFeedItemDto>>> Handle(BrowseNewsQuery request, CancellationToken ct)
    {
        var lang = Enum.TryParse<Language>(request.Language, true, out var l) ? l : Language.En;
        var articles = await repository.BrowsePublishedAsync(null, request.Tags, lang, request.Page, request.PageSize, ct);

        var items = articles.Select(a =>
        {
            var loc = a.Localizations.TryGetValue(lang, out var c) ? c : a.Localizations.Values.FirstOrDefault();
            return new NewsFeedItemDto(a.Id, loc?.Title ?? "", loc?.Summary ?? "", "", a.Tags.Select(t => t.DisplayLabel).ToList(), a.PublishedOnUtc ?? DateTime.MinValue);
        }).ToList();

        return Result.Success(new PagedResult<NewsFeedItemDto>(items, request.Page, request.PageSize, items.Count, items.Count == 0));
    }
}

public sealed class SearchNewsArchiveQueryHandler(IArticleRepository repository) : IQueryHandler<SearchNewsArchiveQuery, PagedResult<NewsSearchResultDto>>
{
    public async Task<Result<PagedResult<NewsSearchResultDto>>> Handle(SearchNewsArchiveQuery request, CancellationToken ct)
    {
        var lang = Enum.TryParse<Language>(request.Language, true, out var l) ? l : Language.En;
        var articles = await repository.SearchArchiveAsync(request.Query, lang, request.DateFrom, request.DateTo, request.Page, request.PageSize, ct);

        var items = articles.Select(a =>
        {
            var loc = a.Localizations.TryGetValue(lang, out var c) ? c : a.Localizations.Values.FirstOrDefault();
            return new NewsSearchResultDto(a.Id, loc?.Title ?? "", loc?.Summary ?? "", a.Status.ToString(), a.PublishedOnUtc);
        }).ToList();

        return Result.Success(new PagedResult<NewsSearchResultDto>(items, request.Page, request.PageSize, items.Count, items.Count == 0));
    }
}

public sealed class GetDashboardNewsQueryHandler(
    IContentPreferenceRepository prefRepository,
    IArticleRepository articleRepository,
    IJobSeekerProfileQueryApi profileApi) : IQueryHandler<GetDashboardNewsQuery, IReadOnlyCollection<DashboardArticleDto>>
{
    public async Task<Result<IReadOnlyCollection<DashboardArticleDto>>> Handle(GetDashboardNewsQuery request, CancellationToken ct)
    {
        var preference = await prefRepository.GetByUserIdAsync(request.UserId, ct);
        var profileAttrs = await profileApi.GetPersonalizationAttributesAsync(request.UserId, ct);

        var candidates = await articleRepository.BrowsePublishedAsync(null, null, preference?.PreferredLanguage ?? Language.En, 1, 100, ct);

        var selector = new DashboardNewsSelector();
        var summaries = candidates.Select(a => new ArticleSummary(a.Id, a.PrimaryCategoryId ?? Guid.Empty, a.PublishedOnUtc ?? DateTime.MinValue, Language.En)).ToList();
        var selectedIds = selector.Select(preference ?? ContentPreference.CreateDefault(request.UserId), profileAttrs, summaries, request.MaxItems).ToHashSet();

        var selected = candidates.Where(a => selectedIds.Contains(a.Id)).ToList();
        var lang = preference?.PreferredLanguage ?? Language.En;

        var dtos = selected.Select(a =>
        {
            var loc = a.Localizations.TryGetValue(lang, out var c) ? c : a.Localizations.Values.FirstOrDefault();
            return new DashboardArticleDto(a.Id, loc?.Title ?? "", loc?.Summary ?? "", "", a.Tags.Select(t => t.DisplayLabel).ToList(), a.PublishedOnUtc ?? DateTime.MinValue);
        }).ToList();

        return Result.Success<IReadOnlyCollection<DashboardArticleDto>>(dtos);
    }
}

public sealed class GetCategoriesQueryHandler(ICategoryRepository repository) : IQueryHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>
{
    public async Task<Result<IReadOnlyCollection<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var categories = await repository.GetAllAsync(ct);
        var dtos = categories.Select(c => new CategoryDto(c.Id, c.Names.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value), c.Slug, c.IsActive)).ToList();
        return Result.Success<IReadOnlyCollection<CategoryDto>>(dtos);
    }
}

public sealed class GetFaqEntryQueryHandler(IFaqEntryRepository repository) : IQueryHandler<GetFaqEntryQuery, FaqEntryDto>
{
    public async Task<Result<FaqEntryDto>> Handle(GetFaqEntryQuery request, CancellationToken ct)
    {
        var entry = await repository.GetByIdAsync(request.FaqEntryId, ct);
        if (entry is null)
            return Result.Failure<FaqEntryDto>(new Error("E-FAQ-NOT-FOUND", "FAQ entry not found."));

        var dto = new FaqEntryDto(
            entry.Id, entry.Kind.ToString(), entry.Status.ToString(),
            entry.Localizations.ToDictionary(kv => kv.Key.ToString(), kv => new FaqContentDto(kv.Value.Question, kv.Value.AnswerRichText)),
            entry.TopicIds.ToList(),
            entry.VisibleRoles?.Roles.Select(r => r.ToString()).ToList() ?? [],
            entry.ContextKeys.ToList(),
            [], entry.CreatedOnUtc, entry.UpdatedOnUtc);
        return Result.Success(dto);
    }
}

public sealed class BrowseHelpCenterQueryHandler(IFaqEntryRepository repository) : IQueryHandler<BrowseHelpCenterQuery, IReadOnlyCollection<HelpEntrySummaryDto>>
{
    public async Task<Result<IReadOnlyCollection<HelpEntrySummaryDto>>> Handle(BrowseHelpCenterQuery request, CancellationToken ct)
    {
        var lang = Enum.TryParse<Language>(request.Language, true, out var l) ? l : Language.En;
        var entries = await repository.BrowsePublishedAsync(request.ViewerRole, lang, ct);
        var dtos = entries.Select(e =>
        {
            var loc = e.Localizations.TryGetValue(lang, out var c) ? c : e.Localizations.Values.FirstOrDefault();
            return new HelpEntrySummaryDto(e.Id, e.Kind.ToString(), loc?.Question ?? "", loc?.AnswerRichText ?? "", []);
        }).ToList();
        return Result.Success<IReadOnlyCollection<HelpEntrySummaryDto>>(dtos);
    }
}

public sealed class SearchHelpContentQueryHandler(IFaqEntryRepository repository) : IQueryHandler<SearchHelpContentQuery, IReadOnlyCollection<HelpSearchResultDto>>
{
    public async Task<Result<IReadOnlyCollection<HelpSearchResultDto>>> Handle(SearchHelpContentQuery request, CancellationToken ct)
    {
        var lang = Enum.TryParse<Language>(request.Language, true, out var l) ? l : Language.En;
        var entries = await repository.SearchPublishedAsync(request.Query, request.ViewerRole, lang, ct);
        var dtos = entries.Select(e =>
        {
            var loc = e.Localizations.TryGetValue(lang, out var c) ? c : e.Localizations.Values.FirstOrDefault();
            return new HelpSearchResultDto(e.Id, e.Kind.ToString(), loc?.Question ?? "", loc?.AnswerRichText ?? "");
        }).ToList();
        return Result.Success<IReadOnlyCollection<HelpSearchResultDto>>(dtos);
    }
}

public sealed class GetContextHelpQueryHandler(IFaqEntryRepository repository) : IQueryHandler<GetContextHelpQuery, IReadOnlyCollection<HelpEntrySummaryDto>>
{
    public async Task<Result<IReadOnlyCollection<HelpEntrySummaryDto>>> Handle(GetContextHelpQuery request, CancellationToken ct)
    {
        var lang = Enum.TryParse<Language>(request.Language, true, out var l) ? l : Language.En;
        var candidates = await repository.GetByContextKeyAsync(request.ContextKey, request.ViewerRole, lang, ct);

        var resolver = new ContextHelpResolver();
        var ids = resolver.Resolve(request.ContextKey, request.ViewerRole ?? "", lang, candidates).ToHashSet();

        var filtered = candidates.Where(e => ids.Contains(e.Id)).ToList();
        var dtos = filtered.Select(e =>
        {
            var loc = e.Localizations.TryGetValue(lang, out var c) ? c : e.Localizations.Values.FirstOrDefault();
            return new HelpEntrySummaryDto(e.Id, e.Kind.ToString(), loc?.Question ?? "", loc?.AnswerRichText ?? "", []);
        }).ToList();

        return Result.Success<IReadOnlyCollection<HelpEntrySummaryDto>>(dtos);
    }
}

public sealed class GetGuidedToursQueryHandler(IGuidedTourRepository repository) : IQueryHandler<GetGuidedToursQuery, IReadOnlyCollection<GuidedTourDto>>
{
    public async Task<Result<IReadOnlyCollection<GuidedTourDto>>> Handle(GetGuidedToursQuery request, CancellationToken ct)
    {
        var audience = Enum.TryParse<Audience>(request.Audience, true, out var a) ? a : Audience.NewUsers;
        var lang = Enum.TryParse<Language>(request.Language, true, out var l) ? l : Language.En;
        var tours = await repository.GetActiveAsync(audience, lang, ct);
        var dtos = tours.Select(t => new GuidedTourDto(
            t.Id, t.Language.ToString(), t.Name, t.Description,
            t.TargetAudience.Audiences.Select(a => a.ToString()).ToList(),
            t.Steps.Select(s => new TourStepDto(s.Id, s.Order, s.TargetSelector, s.TooltipText, s.Action?.Kind.ToString(), s.Action?.Payload)).ToList()
        )).ToList();
        return Result.Success<IReadOnlyCollection<GuidedTourDto>>(dtos);
    }
}

public sealed class GetFeedbackSummaryQueryHandler(IHelpFeedbackRepository repository) : IQueryHandler<GetFeedbackSummaryQuery, IReadOnlyCollection<FeedbackSummaryDto>>
{
    public async Task<Result<IReadOnlyCollection<FeedbackSummaryDto>>> Handle(GetFeedbackSummaryQuery request, CancellationToken ct)
    {
        // Simplified — returns empty for now. Full implementation aggregates per FaqEntry.
        return Result.Success<IReadOnlyCollection<FeedbackSummaryDto>>([]);
    }
}
