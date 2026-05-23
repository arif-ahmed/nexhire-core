using System.Text.RegularExpressions;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Domain.ValueObjects;

public sealed class TermCode : ValueObject
{
    private static readonly Regex AllowedCharactersRegex = new("^[A-Z0-9.\\-_]+$", RegexOptions.Compiled);

    public string Value { get; }

    private TermCode(string value)
    {
        Value = value;
    }

    public static Result<TermCode> Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<TermCode>(new Error("TermCode.Empty", "Term code cannot be empty."));
        }

        if (code.Length > 64)
        {
            return Result.Failure<TermCode>(new Error("TermCode.TooLong", "Term code cannot exceed 64 characters."));
        }

        var normalizedValue = code.Trim().ToUpperInvariant();

        if (!normalizedValue.Contains('.'))
        {
            return Result.Failure<TermCode>(new Error("TermCode.InvalidFormat", "Term code must contain a namespace separator dot (e.g. SKILL.PYTHON)."));
        }

        if (!AllowedCharactersRegex.IsMatch(normalizedValue))
        {
            return Result.Failure<TermCode>(new Error("TermCode.InvalidCharacters", "Term code contains invalid characters. Only uppercase alphanumeric, dots, hyphens, and underscores are allowed."));
        }

        return Result.Success(new TermCode(normalizedValue));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
