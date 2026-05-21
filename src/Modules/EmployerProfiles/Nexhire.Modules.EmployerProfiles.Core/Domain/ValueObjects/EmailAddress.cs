using System.Text.RegularExpressions;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public class EmailAddress : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static Result<EmailAddress> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<EmailAddress>(new Error("EmailAddress.Empty", "Email address cannot be empty."));
        }

        var trimmed = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(trimmed))
        {
            return Result.Failure<EmailAddress>(new Error("EmailAddress.Invalid", "The email format is invalid."));
        }

        return Result.Success(new EmailAddress(trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
