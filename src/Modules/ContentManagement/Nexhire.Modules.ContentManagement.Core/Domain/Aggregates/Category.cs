using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;

public sealed class Category : AggregateRoot<Guid>
{
    private Dictionary<Language, string> _names = new();
    public IReadOnlyDictionary<Language, string> Names => _names;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private Category() : base() { }

    private Category(Guid id, Dictionary<Language, string> names, string slug) : base(id)
    {
        _names = names;
        Slug = slug;
        IsActive = true;
        CreatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public static Category Create(Dictionary<Language, string> names, string slug)
    {
        var trimmedSlug = slug.Trim().ToLowerInvariant();
        return new Category(Guid.NewGuid(), names, trimmedSlug);
    }

    public void Rename(Language language, string name)
    {
        _names[language] = name.Trim();
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void SetSlug(string slug)
    {
        Slug = slug.Trim().ToLowerInvariant();
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Result EnsureDeletable(int referenceCount)
    {
        if (referenceCount > 0)
            return Result.Failure(new Error("E-CATEGORY-IN-USE", "Cannot delete a category that is referenced by articles."));

        return Result.Success();
    }
}
