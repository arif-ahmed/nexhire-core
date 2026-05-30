using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

public class PasswordHash : ValueObject
{
    public string Algorithm { get; }
    public string Value { get; }

    private PasswordHash(string algorithm, string value)
    {
        Algorithm = algorithm;
        Value = value;
    }

    public static Result<PasswordHash> Create(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return Result.Failure<PasswordHash>(new Error("PasswordHash.Empty", "Password hash cannot be empty."));

        if (!hash.StartsWith("$argon2id$"))
            return Result.Failure<PasswordHash>(new Error("PasswordHash.InvalidAlgorithm", "Password hash must use argon2id algorithm."));

        return new PasswordHash("argon2id", hash);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
