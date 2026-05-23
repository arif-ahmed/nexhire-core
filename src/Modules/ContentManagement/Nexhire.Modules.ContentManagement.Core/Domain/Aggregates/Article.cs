using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.Events;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;

public sealed class Article : AggregateRoot<Guid>
{
    private readonly Dictionary<Language, LocalizedContent> _localizations = new();
    private readonly List<ArticleTag> _tags = new();
    private readonly List<MediaReference> _media = new();

    public Guid AuthorUserId { get; private set; }
    public ArticleStatus Status { get; private set; }
    public Guid? PrimaryCategoryId { get; private set; }
    public IReadOnlyDictionary<Language, LocalizedContent> Localizations => _localizations;
    public IReadOnlyList<ArticleTag> Tags => _tags.AsReadOnly();
    public IReadOnlyList<MediaReference> Media => _media.AsReadOnly();
    public PublicationSchedule? PublicationSchedule { get; private set; }
    public DateTime? PublishedOnUtc { get; private set; }
    public ArticleStatus? PreviousStatus { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private Article() : base() { }

    private Article(Guid id, Guid authorUserId, Language language, LocalizedContent content) : base(id)
    {
        AuthorUserId = authorUserId;
        Status = ArticleStatus.Draft;
        _localizations[language] = content;
        CreatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public static Article CreateDraft(Guid authorUserId, Language language, LocalizedContent content)
    {
        return new Article(Guid.NewGuid(), authorUserId, language, content);
    }

    // Localization
    public void SetLocalization(Language language, LocalizedContent content)
    {
        _localizations[language] = content;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Result RemoveLocalization(Language language)
    {
        if (_localizations.Count <= 1)
            return Result.Failure(new Error("E-ARTICLE-LAST-LOCALIZATION", "Cannot remove the last remaining localization."));

        _localizations.Remove(language);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    // Category
    public void SetPrimaryCategory(Guid categoryId)
    {
        PrimaryCategoryId = categoryId;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    // Tags
    public void SetTags(Language language, IEnumerable<string> tagLabels)
    {
        _tags.RemoveAll(t => t.Language == language);
        foreach (var label in tagLabels)
        {
            var tagResult = ArticleTag.Create(language, label);
            if (tagResult.IsSuccess && !_tags.Any(t => t.Language == language && t.NormalizedLabel == tagResult.Value.NormalizedLabel))
                _tags.Add(tagResult.Value);
        }
        UpdatedOnUtc = DateTime.UtcNow;
    }

    // Media
    public void AddMedia(MediaReference media)
    {
        _media.Add(media);
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Result RemoveMedia(string storageKey)
    {
        var removed = _media.RemoveAll(m => m.StorageKey == storageKey);
        if (removed == 0)
            return Result.Failure(new Error("E-MEDIA-NOT-FOUND", $"Media with storage key '{storageKey}' not found."));

        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    // Publish
    public Result Publish()
    {
        if (Status == ArticleStatus.Published)
            return Result.Success(); // Idempotent

        if (!CanPublishFrom(Status))
            return Result.Failure(new Error("E-ARTICLE-ILLEGAL-TRANSITION", $"Cannot publish from status '{Status}'."));

        var gateResult = ValidatePublishGate();
        if (gateResult.IsFailure)
            return gateResult;

        Status = ArticleStatus.Published;
        PublishedOnUtc ??= DateTime.UtcNow;
        PublicationSchedule = null;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ArticlePublishedDomainEvent(Id));
        return Result.Success();
    }

    // Schedule
    public Result Schedule(PublicationSchedule schedule)
    {
        if (Status != ArticleStatus.Draft && Status != ArticleStatus.Scheduled)
            return Result.Failure(new Error("E-ARTICLE-ILLEGAL-TRANSITION", $"Cannot schedule from status '{Status}'."));

        var gateResult = ValidatePublishGate();
        if (gateResult.IsFailure)
            return gateResult;

        Status = ArticleStatus.Scheduled;
        PublicationSchedule = schedule;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ArticleScheduledDomainEvent(Id, schedule.PublishAtUtc));
        return Result.Success();
    }

    public Result CancelSchedule()
    {
        if (Status != ArticleStatus.Scheduled)
            return Result.Failure(new Error("E-ARTICLE-ILLEGAL-TRANSITION", "Cannot cancel schedule: article is not scheduled."));

        Status = ArticleStatus.Draft;
        PublicationSchedule = null;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkPublishedBySchedule()
    {
        if (Status != ArticleStatus.Scheduled)
            return Result.Failure(new Error("E-ARTICLE-ILLEGAL-TRANSITION", "Article is not scheduled."));

        Status = ArticleStatus.Published;
        PublishedOnUtc ??= DateTime.UtcNow;
        PublicationSchedule = null;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ArticlePublishedDomainEvent(Id));
        return Result.Success();
    }

    public bool IsDueForPublication(DateTime nowUtc) =>
        Status == ArticleStatus.Scheduled && PublicationSchedule is not null && PublicationSchedule.PublishAtUtc <= nowUtc;

    // Unpublish
    public Result Unpublish()
    {
        if (Status != ArticleStatus.Published)
            return Result.Failure(new Error("E-ARTICLE-ILLEGAL-TRANSITION", "Cannot unpublish: article is not published."));

        Status = ArticleStatus.Unpublished;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ArticleArchivedDomainEvent(Id, ArticleStatus.Unpublished));
        return Result.Success();
    }

    // Archive
    public Result Archive()
    {
        if (Status != ArticleStatus.Unpublished && Status != ArticleStatus.Published)
            return Result.Failure(new Error("E-ARTICLE-ILLEGAL-TRANSITION", $"Cannot archive from status '{Status}'."));

        PreviousStatus = Status;
        Status = ArticleStatus.Archived;
        PublicationSchedule = null;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ArticleArchivedDomainEvent(Id, ArticleStatus.Archived));
        return Result.Success();
    }

    // Restore
    public Result RestoreFromArchive()
    {
        if (Status != ArticleStatus.Archived)
            return Result.Failure(new Error("E-ARTICLE-ILLEGAL-TRANSITION", "Cannot restore: article is not archived."));

        var restoredStatus = PreviousStatus ?? ArticleStatus.Draft;
        Status = restoredStatus;
        PreviousStatus = null;
        UpdatedOnUtc = DateTime.UtcNow;

        if (Status == ArticleStatus.Published)
            RaiseDomainEvent(new ArticlePublishedDomainEvent(Id));

        return Result.Success();
    }

    private static bool CanPublishFrom(ArticleStatus status) =>
        status is ArticleStatus.Draft or ArticleStatus.Scheduled or ArticleStatus.Unpublished;

    private Result ValidatePublishGate()
    {
        if (PrimaryCategoryId is null)
            return Result.Failure(new Error("E-ARTICLE-NO-CATEGORY", "Article must have a primary category to publish."));

        if (_localizations.Count == 0)
            return Result.Failure(new Error("E-ARTICLE-NO-LOCALIZATION", "Article must have at least one localization to publish."));

        return Result.Success();
    }
}
