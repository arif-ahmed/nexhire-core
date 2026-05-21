using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public class WebsiteUrl : ValueObject
{
    public string Value { get; }

    private WebsiteUrl(string value)
    {
        Value = value;
    }

    public static Result<WebsiteUrl> Create(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result.Failure<WebsiteUrl>(new Error("WebsiteUrl.Empty", "Website URL cannot be empty."));
        }

        var trimmed = url.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uriResult) || 
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure<WebsiteUrl>(new Error("WebsiteUrl.Invalid", "The website URL is invalid. Must be a valid absolute http or https URL."));
        }

        return Result.Success(new WebsiteUrl(trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
