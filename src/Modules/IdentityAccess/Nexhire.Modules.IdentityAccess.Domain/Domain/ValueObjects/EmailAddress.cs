using System.Text.RegularExpressions;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

public class EmailAddress : ValueObject
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static Result<EmailAddress> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<EmailAddress>(new Error("Email.Empty", "Email address cannot be empty."));

        var trimmedEmail = email.Trim();

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        if (!emailRegex.IsMatch(trimmedEmail))
            return Result.Failure<EmailAddress>(new Error("Email.Invalid", "The email format is invalid."));

        if (trimmedEmail.Length < 5)
            return Result.Failure<EmailAddress>(new Error("Email.Invalid", "The email format is invalid."));

        return new EmailAddress(trimmedEmail.ToLowerInvariant());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
