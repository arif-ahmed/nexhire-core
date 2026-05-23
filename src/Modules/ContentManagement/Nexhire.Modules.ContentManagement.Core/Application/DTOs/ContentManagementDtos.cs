namespace Nexhire.Modules.ContentManagement.Core.Application.DTOs;

// Article DTOs
public sealed record ArticleDto(
    Guid Id,
    Guid AuthorUserId,
    string Status,
    Guid? PrimaryCategoryId,
    string? CategorySlug,
    IReadOnlyDictionary<string, LocalizedContentDto> Localizations,
    IReadOnlyCollection<ArticleTagDto> Tags,
    IReadOnlyCollection<MediaRefDto> Media,
    string? SchedulePublishAtUtc,
    DateTime? PublishedOnUtc,
    DateTime CreatedOnUtc,
    DateTime UpdatedOnUtc);

public sealed record LocalizedContentDto(string Title, string Summary, string BodyRichText);
public sealed record ArticleTagDto(string Language, string NormalizedLabel, string DisplayLabel);
public sealed record MediaRefDto(string StorageKey, string Url, string MimeType, long SizeBytes, string Kind, string? TranscriptUrl);

public sealed record CreateArticleDraftResponse(Guid ArticleId);

// News feed
public sealed record NewsFeedItemDto(
    Guid ArticleId,
    string Title,
    string Summary,
    string CategorySlug,
    IReadOnlyCollection<string> Tags,
    DateTime PublishedOnUtc);

public sealed record NewsSearchResultDto(
    Guid ArticleId,
    string Title,
    string Summary,
    string Status,
    DateTime? PublishedOnUtc);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool NoResults);

// Category DTOs
public sealed record CategoryDto(
    Guid Id,
    IReadOnlyDictionary<string, string> Names,
    string Slug,
    bool IsActive);

// FAQ DTOs
public sealed record FaqEntryDto(
    Guid Id,
    string Kind,
    string Status,
    IReadOnlyDictionary<string, FaqContentDto> Localizations,
    IReadOnlyCollection<Guid> TopicIds,
    IReadOnlyCollection<string> VisibleRoles,
    IReadOnlyCollection<string> ContextKeys,
    IReadOnlyCollection<MultimediaBlockDto> MultimediaBlocks,
    DateTime CreatedOnUtc,
    DateTime UpdatedOnUtc);

public sealed record FaqContentDto(string Question, string AnswerRichText);

public sealed record HelpEntrySummaryDto(
    Guid Id,
    string Kind,
    string Question,
    string Answer,
    IReadOnlyCollection<string> Topics);

public sealed record HelpSearchResultDto(
    Guid Id,
    string Kind,
    string Question,
    string HighlightSnippet);

public sealed record MultimediaBlockDto(string BlockKind, MediaRefDto? Media, IReadOnlyCollection<GuideStepDto>? Steps);
public sealed record GuideStepDto(int Order, string Caption, MediaRefDto? Image);

// Topic DTOs
public sealed record TopicDto(Guid Id, IReadOnlyDictionary<string, string> Names, string Slug, bool IsActive);

// Guided Tour DTOs
public sealed record GuidedTourDto(
    Guid Id,
    string Language,
    string Name,
    string Description,
    IReadOnlyCollection<string> TargetAudience,
    IReadOnlyCollection<TourStepDto> Steps);

public sealed record TourStepDto(
    Guid StepId,
    int Order,
    string TargetSelector,
    string TooltipText,
    string? ActionKind,
    string? ActionPayload);

// Feedback DTOs
public sealed record FeedbackSummaryDto(
    Guid FaqEntryId,
    int HelpfulCount,
    int NotHelpfulCount,
    IReadOnlyDictionary<string, int> ReasonBreakdown,
    IReadOnlyCollection<string> RecentComments);

// Content Preference DTOs
public sealed record ContentPreferenceDto(
    Guid Id,
    Guid UserId,
    string PreferredLanguage,
    IReadOnlyCollection<Guid> IncludedCategoryIds,
    IReadOnlyCollection<Guid> HiddenCategoryIds);

// Dashboard
public sealed record DashboardArticleDto(
    Guid ArticleId,
    string Title,
    string Summary,
    string CategorySlug,
    IReadOnlyCollection<string> Tags,
    DateTime PublishedOnUtc);
