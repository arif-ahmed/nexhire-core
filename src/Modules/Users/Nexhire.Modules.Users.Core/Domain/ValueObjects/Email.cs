using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Users.Core.Domain.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Email>(new Error("Email.Empty", "Email address cannot be empty."));

        if (!email.Contains("@") || email.Length < 3)
            return Result.Failure<Email>(new Error("Email.Invalid", "The email format is invalid."));

        return new Email(email.Trim().ToLowerInvariant());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
