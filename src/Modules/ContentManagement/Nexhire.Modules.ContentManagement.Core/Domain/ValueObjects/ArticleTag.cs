using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

public sealed class ArticleTag : ValueObject
{
    public Language Language { get; }
    public string NormalizedLabel { get; }
    public string DisplayLabel { get; }

    private ArticleTag(Language language, string normalizedLabel, string displayLabel)
    {
        Language = language;
        NormalizedLabel = normalizedLabel;
        DisplayLabel = displayLabel;
    }

    public static Result<ArticleTag> Create(Language language, string displayLabel)
    {
        if (string.IsNullOrWhiteSpace(displayLabel))
            return Result.Failure<ArticleTag>(new Error("E-TAG-EMPTY", "Tag label cannot be empty."));

        if (displayLabel.Length > 50)
            return Result.Failure<ArticleTag>(new Error("E-TAG-TOO-LONG", "Tag label cannot exceed 50 characters."));

        var trimmed = displayLabel.Trim();
        var normalized = trimmed.ToLowerInvariant();

        return Result.Success(new ArticleTag(language, normalized, trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Language;
        yield return NormalizedLabel;
    }
}
