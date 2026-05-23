using Nexhire.Modules.ContentManagement.Core.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.ContentManagement.Core.Application.Commands;

// --- Article ---
public sealed record CreateArticleDraftCommand(
    Guid AuthorUserId,
    string Language,
    string Title,
    string Summary,
    string BodyRichText) : ICommand<CreateArticleDraftResponse>;

public sealed record UpdateArticleContentCommand(
    Guid ArticleId,
    string Language,
    string Title,
    string Summary,
    string BodyRichText) : ICommand;

public sealed record RemoveArticleLocalizationCommand(
    Guid ArticleId,
    string Language) : ICommand;

public sealed record SetArticleCategoryCommand(
    Guid ArticleId,
    Guid CategoryId) : ICommand;

public sealed record SetArticleTagsCommand(
    Guid ArticleId,
    string Language,
    IReadOnlyList<string> Tags) : ICommand;

public sealed record AddArticleMediaCommand(
    Guid ArticleId,
    string StorageKey,
    string Url,
    string MimeType,
    long SizeBytes,
    string Kind) : ICommand;

public sealed record RemoveArticleMediaCommand(
    Guid ArticleId,
    string StorageKey) : ICommand;

public sealed record PublishArticleCommand(Guid ArticleId) : ICommand;

public sealed record ScheduleArticleCommand(
    Guid ArticleId,
    DateTime PublishAtUtc) : ICommand;

public sealed record CancelArticleScheduleCommand(Guid ArticleId) : ICommand;

public sealed record RescheduleArticleCommand(
    Guid ArticleId,
    DateTime PublishAtUtc) : ICommand;

public sealed record UnpublishArticleCommand(Guid ArticleId) : ICommand;

public sealed record ArchiveArticleCommand(Guid ArticleId) : ICommand;

public sealed record BulkArchiveArticlesCommand(IReadOnlyList<Guid> ArticleIds) : ICommand;

public sealed record RestoreArticleFromArchiveCommand(Guid ArticleId) : ICommand;

public sealed record PublishDueArticlesCommand() : ICommand;

// --- Category ---
public sealed record CreateCategoryCommand(
    IReadOnlyDictionary<string, string> Names,
    string Slug) : ICommand<CreateArticleDraftResponse>;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    IReadOnlyDictionary<string, string>? Names,
    string? Slug) : ICommand;

public sealed record DeactivateCategoryCommand(Guid CategoryId) : ICommand;
public sealed record DeleteCategoryCommand(Guid CategoryId) : ICommand;

// --- Topic ---
public sealed record CreateTopicCommand(
    IReadOnlyDictionary<string, string> Names,
    string Slug) : ICommand<CreateArticleDraftResponse>;

public sealed record UpdateTopicCommand(
    Guid TopicId,
    IReadOnlyDictionary<string, string>? Names,
    string? Slug) : ICommand;

public sealed record DeleteTopicCommand(Guid TopicId) : ICommand;

// --- FaqEntry ---
public sealed record CreateFaqEntryCommand(
    string Kind,
    string Language,
    string Question,
    string AnswerRichText) : ICommand<CreateArticleDraftResponse>;

public sealed record UpdateFaqEntryContentCommand(
    Guid FaqEntryId,
    string Language,
    string Question,
    string AnswerRichText) : ICommand;

public sealed record SetFaqTopicsCommand(
    Guid FaqEntryId,
    IReadOnlyList<Guid> TopicIds) : ICommand;

public sealed record SetFaqVisibleRolesCommand(
    Guid FaqEntryId,
    IReadOnlyList<string> Roles) : ICommand;

public sealed record SetFaqContextKeysCommand(
    Guid FaqEntryId,
    IReadOnlyList<string> ContextKeys) : ICommand;

public sealed record AddFaqMultimediaBlockCommand(
    Guid FaqEntryId,
    string BlockKind,
    string? MediaStorageKey,
    string? MediaUrl,
    string? MediaMimeType,
    long? MediaSizeBytes,
    string? MediaKind) : ICommand;

public sealed record PublishFaqEntryCommand(Guid FaqEntryId) : ICommand;
public sealed record UnpublishFaqEntryCommand(Guid FaqEntryId) : ICommand;

// --- GuidedTour ---
public sealed record CreateGuidedTourCommand(
    string Language,
    string Name,
    string Description,
    IReadOnlyList<string> TargetAudience) : ICommand<CreateArticleDraftResponse>;

public sealed record AddTourStepCommand(
    Guid TourId,
    string TargetSelector,
    string TooltipText,
    string? ActionKind,
    string? ActionPayload) : ICommand;

public sealed record UpdateTourStepCommand(
    Guid TourId,
    Guid StepId,
    string TargetSelector,
    string TooltipText,
    string? ActionKind,
    string? ActionPayload) : ICommand;

public sealed record RemoveTourStepCommand(Guid TourId, Guid StepId) : ICommand;

public sealed record ReorderTourStepsCommand(
    Guid TourId,
    IReadOnlyList<Guid> OrderedStepIds) : ICommand;

public sealed record SetTourActiveCommand(Guid TourId, bool IsActive) : ICommand;

// --- HelpFeedback ---
public sealed record SubmitHelpFeedbackCommand(
    Guid FaqEntryId,
    bool WasHelpful,
    string? Reason,
    string? Comment,
    string? SubmittedByRole,
    string Language) : ICommand;

// --- ContentPreference ---
public sealed record UpdateContentPreferenceCommand(
    Guid UserId,
    string? PreferredLanguage,
    IReadOnlyList<Guid>? IncludedCategoryIds,
    IReadOnlyList<Guid>? HiddenCategoryIds) : ICommand;

// --- Integration event consumer ---
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Role,
    string Email,
    DateTime CreatedAtUtc) : ICommand;
