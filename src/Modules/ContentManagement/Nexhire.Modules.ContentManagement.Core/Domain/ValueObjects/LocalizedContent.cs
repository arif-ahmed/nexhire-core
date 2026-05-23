using System.Text.RegularExpressions;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

public sealed class LocalizedContent : ValueObject
{
    private static readonly Regex ScriptTagRegex = new(
        @"<script[^>]*>[\s\S]*?</script>|<style[^>]*>[\s\S]*?</style>|\bon\w+\s*=",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Title { get; }
    public string Summary { get; }
    public string BodyRichText { get; }

    private LocalizedContent(string title, string summary, string bodyRichText)
    {
        Title = title;
        Summary = summary;
        BodyRichText = bodyRichText;
    }

    public static Result<LocalizedContent> Create(string title, string summary, string bodyRichText)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<LocalizedContent>(new Error("E-CONTENT-TITLE-EMPTY", "Title cannot be empty."));

        if (title.Length > 200)
            return Result.Failure<LocalizedContent>(new Error("E-CONTENT-TITLE-TOO-LONG", "Title cannot exceed 200 characters."));

        if (summary.Length > 500)
            return Result.Failure<LocalizedContent>(new Error("E-CONTENT-SUMMARY-TOO-LONG", "Summary cannot exceed 500 characters."));

        if (string.IsNullOrWhiteSpace(bodyRichText))
            return Result.Failure<LocalizedContent>(new Error("E-CONTENT-BODY-EMPTY", "Body cannot be empty."));

        var sanitized = SanitizeHtml(bodyRichText);

        return Result.Success(new LocalizedContent(title.Trim(), summary.Trim(), sanitized));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Title;
        yield return Summary;
        yield return BodyRichText;
    }

    private static string SanitizeHtml(string html)
    {
        return ScriptTagRegex.Replace(html, string.Empty);
    }
}
