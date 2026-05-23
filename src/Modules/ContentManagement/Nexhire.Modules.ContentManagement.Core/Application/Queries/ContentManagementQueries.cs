using Nexhire.Modules.ContentManagement.Core.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.ContentManagement.Core.Application.Queries;

public sealed record GetArticleQuery(Guid ArticleId) : IQuery<ArticleDto>;

public sealed record BrowseNewsQuery(
    string? CategorySlug,
    IReadOnlyList<string>? Tags,
    string Language,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<NewsFeedItemDto>>;

public sealed record SearchNewsArchiveQuery(
    string Query,
    string Language,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<NewsSearchResultDto>>;

public sealed record GetDashboardNewsQuery(
    Guid UserId,
    int MaxItems = 10) : IQuery<IReadOnlyCollection<DashboardArticleDto>>;

public sealed record GetCategoriesQuery() : IQuery<IReadOnlyCollection<CategoryDto>>;

public sealed record GetFaqEntryQuery(Guid FaqEntryId) : IQuery<FaqEntryDto>;

public sealed record BrowseHelpCenterQuery(
    string? ViewerRole,
    string Language) : IQuery<IReadOnlyCollection<HelpEntrySummaryDto>>;

public sealed record SearchHelpContentQuery(
    string Query,
    string? ViewerRole,
    string Language) : IQuery<IReadOnlyCollection<HelpSearchResultDto>>;

public sealed record GetContextHelpQuery(
    string ContextKey,
    string? ViewerRole,
    string Language) : IQuery<IReadOnlyCollection<HelpEntrySummaryDto>>;

public sealed record GetGuidedToursQuery(
    string Audience,
    string Language) : IQuery<IReadOnlyCollection<GuidedTourDto>>;

public sealed record GetFeedbackSummaryQuery() : IQuery<IReadOnlyCollection<FeedbackSummaryDto>>;
