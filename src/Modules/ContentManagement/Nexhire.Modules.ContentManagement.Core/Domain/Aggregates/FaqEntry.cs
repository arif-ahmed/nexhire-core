using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.Events;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;

public sealed class FaqEntry : AggregateRoot<Guid>
{
    private readonly Dictionary<Language, FaqContent> _localizations = new();
    private readonly List<Guid> _topicIds = new();
    private readonly List<string> _contextKeys = new();
    private readonly List<MultimediaBlock> _multimediaBlocks = new();

    public FaqEntryKind Kind { get; private set; }
    public ContentStatus Status { get; private set; }
    public IReadOnlyDictionary<Language, FaqContent> Localizations => _localizations;
    public IReadOnlyList<Guid> TopicIds => _topicIds.AsReadOnly();
    public VisibleRoleSet? VisibleRoles { get; private set; }
    public IReadOnlyList<string> ContextKeys => _contextKeys.AsReadOnly();
    public IReadOnlyList<MultimediaBlock> MultimediaBlocks => _multimediaBlocks.AsReadOnly();
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private FaqEntry() : base() { }

    private FaqEntry(Guid id, FaqEntryKind kind, Language language, FaqContent content) : base(id)
    {
        Kind = kind;
        Status = ContentStatus.Draft;
        _localizations[language] = content;
        CreatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public static FaqEntry CreateDraft(FaqEntryKind kind, Language language, FaqContent content)
    {
        return new FaqEntry(Guid.NewGuid(), kind, language, content);
    }

    public void SetLocalization(Language language, FaqContent content)
    {
        _localizations[language] = content;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Result RemoveLocalization(Language language)
    {
        if (_localizations.Count <= 1)
            return Result.Failure(new Error("E-FAQ-LAST-LOCALIZATION", "Cannot remove the last localization."));

        _localizations.Remove(language);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void SetTopics(IEnumerable<Guid> topicIds)
    {
        _topicIds.Clear();
        _topicIds.AddRange(topicIds.Distinct());
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void SetVisibleRoles(VisibleRoleSet visibleRoles)
    {
        VisibleRoles = visibleRoles;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void SetContextKeys(IEnumerable<string> keys)
    {
        _contextKeys.Clear();
        _contextKeys.AddRange(keys.Select(k => k.Trim()).Where(k => k.Length > 0).Distinct());
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Result AddMultimediaBlock(MultimediaBlock block)
    {
        if (Kind != FaqEntryKind.HelpArticle)
            return Result.Failure(new Error("E-FAQ-NOT-HELP-ARTICLE", "Multimedia blocks can only be added to HelpArticle entries."));

        _multimediaBlocks.Add(block);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RemoveMultimediaBlock(int index)
    {
        if (index < 0 || index >= _multimediaBlocks.Count)
            return Result.Failure(new Error("E-FAQ-BLOCK-NOT-FOUND", "Multimedia block index out of range."));

        _multimediaBlocks.RemoveAt(index);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Publish()
    {
        if (Status == ContentStatus.Published)
            return Result.Success(); // Idempotent

        if (_localizations.Count == 0)
            return Result.Failure(new Error("E-FAQ-NO-LOCALIZATION", "FAQ entry must have at least one localization."));

        Status = ContentStatus.Published;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new FaqPublishedDomainEvent(Id, Kind));
        return Result.Success();
    }

    public Result Unpublish()
    {
        if (Status != ContentStatus.Published)
            return Result.Failure(new Error("E-FAQ-ILLEGAL-TRANSITION", "Cannot unpublish: entry is not published."));

        Status = ContentStatus.Draft;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }
}
