using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

public class RawPassword : ValueObject
{
    public string Value { get; }

    private RawPassword(string value)
    {
        Value = value;
    }

    public static Result<RawPassword> Create(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Result.Failure<RawPassword>(new Error("Password.Empty", "Password cannot be empty."));

        if (password.Length < 10)
            return Result.Failure<RawPassword>(new Error("Password.TooShort", "Password must be at least 10 characters long."));

        var hasLower = password.Any(char.IsLower);
        var hasUpper = password.Any(char.IsUpper);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        var characterClassCount = new[] { hasLower, hasUpper, hasDigit, hasSpecial }.Count(c => c);

        if (characterClassCount < 3)
            return Result.Failure<RawPassword>(new Error("Password.MissingCharacterClass", "Password must contain at least 3 character classes (lowercase, uppercase, digit, special)."));

        if (HasTrivialSequence(password))
            return Result.Failure<RawPassword>(new Error("Password.WeakSequence", "Password contains a trivial sequence."));

        return new RawPassword(password);
    }

    private static bool HasTrivialSequence(string password)
    {
        var lower = password.ToLower();

        var sequences = new[]
        {
            "abcd", "bcde", "cdef", "defg", "efgh", "fghi", "ghij", "hijk", "ijkl", "jklm", "klmn", "lmno", "mnop", "nopq", "opqr", "pqrs", "qrst", "rstu", "stuv", "tuvw", "uvwx", "vwxy", "wxyz",
            "0123", "1234", "2345", "3456", "4567", "5678", "6789",
            "dcba", "edcb", "fdec", "gedf", "hfge", "ighf", "jihg", "kjih", "lkji", "mlkj", "nlkm", "olmn", "polm", "qpon", "rqpo", "srqp", "tsrq", "utsr", "vuts", "wvut", "xwvu", "yxwv", "zyxw",
            "9876", "8765", "7654", "6543", "5432", "4321", "3210"
        };

        foreach (var seq in sequences)
        {
            if (lower.Contains(seq))
                return true;
        }

        return false;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
