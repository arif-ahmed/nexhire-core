using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;

public sealed class ContentPreference : AggregateRoot<Guid>
{
    private readonly List<Guid> _includedCategoryIds = new();
    private readonly List<Guid> _hiddenCategoryIds = new();

    public Guid UserId { get; private set; }
    public Language PreferredLanguage { get; private set; }
    public IReadOnlyList<Guid> IncludedCategoryIds => _includedCategoryIds.AsReadOnly();
    public IReadOnlyList<Guid> HiddenCategoryIds => _hiddenCategoryIds.AsReadOnly();
    public DateTime UpdatedOnUtc { get; private set; }

    private ContentPreference() : base() { }

    private ContentPreference(Guid id, Guid userId) : base(id)
    {
        UserId = userId;
        PreferredLanguage = Language.En;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public static ContentPreference CreateDefault(Guid userId)
    {
        return new ContentPreference(Guid.NewGuid(), userId);
    }

    public void SetPreferredLanguage(Language language)
    {
        PreferredLanguage = language;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Result SetIncludedCategories(IEnumerable<Guid> categoryIds)
    {
        var ids = categoryIds.ToList();
        if (_hiddenCategoryIds.Any(h => ids.Contains(h)))
            return Result.Failure(new Error("E-PREFERENCE-OVERLAP", "A category cannot be in both included and hidden lists."));

        _includedCategoryIds.Clear();
        _includedCategoryIds.AddRange(ids.Distinct());
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result SetHiddenCategories(IEnumerable<Guid> categoryIds)
    {
        var ids = categoryIds.ToList();
        if (_includedCategoryIds.Any(i => ids.Contains(i)))
            return Result.Failure(new Error("E-PREFERENCE-OVERLAP", "A category cannot be in both included and hidden lists."));

        _hiddenCategoryIds.Clear();
        _hiddenCategoryIds.AddRange(ids.Distinct());
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }
}
