using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Services;

public static class PasswordPolicyService
{
    /// <summary>
    /// Enforces invariant #4: min length 10, ≥3 character classes, no trivial sequences.
    /// errorCodePrefix is "E-REG" for provisioning/change and "E-RESET" for the reset path.
    /// </summary>
    public static Result Validate(RawPassword candidate, string errorCodePrefix = "E-REG")
    {
        var value = candidate.Value;

        if (value.Length < 10)
            return Result.Failure(new Error(
                $"{errorCodePrefix}-INVALID-PASSWORD",
                "Password must be at least 10 characters long."));

        var hasLower   = value.Any(char.IsLower);
        var hasUpper   = value.Any(char.IsUpper);
        var hasDigit   = value.Any(char.IsDigit);
        var hasSymbol  = value.Any(c => !char.IsLetterOrDigit(c));
        var classCount = new[] { hasLower, hasUpper, hasDigit, hasSymbol }.Count(b => b);

        if (classCount < 3)
            return Result.Failure(new Error(
                $"{errorCodePrefix}-INVALID-PASSWORD",
                "Password must contain at least 3 character classes (lowercase, uppercase, digit, symbol)."));

        if (HasTrivialSequence(value))
            return Result.Failure(new Error(
                $"{errorCodePrefix}-INVALID-PASSWORD",
                "Password contains a trivial repeating or sequential pattern."));

        return Result.Success();
    }

    private static bool HasTrivialSequence(string password)
    {
        var lower = password.ToLowerInvariant();

        // Detect runs of the same character (e.g. "aaaaaaaaaa")
        for (var i = 0; i <= lower.Length - 4; i++)
        {
            if (lower[i] == lower[i + 1] && lower[i + 1] == lower[i + 2] && lower[i + 2] == lower[i + 3])
                return true;
        }

        // Detect keyboard/alphabetic sequential runs (ascending and descending)
        var sequences = new[]
        {
            "abcd","bcde","cdef","defg","efgh","fghi","ghij","hijk","ijkl","jklm",
            "klmn","lmno","mnop","nopq","opqr","pqrs","qrst","rstu","stuv","tuvw",
            "uvwx","vwxy","wxyz",
            "0123","1234","2345","3456","4567","5678","6789",
            "dcba","edcb","fedc","gfed","hgfe","ihgf","jihg","kjih","lkji","mlkj",
            "nmkl","onml","ponm","qpon","rqpo","srqp","tsrq","utsr","vuts","wvut",
            "xwvu","yxwv","zyxw",
            "9876","8765","7654","6543","5432","4321","3210"
        };

        foreach (var seq in sequences)
        {
            if (lower.Contains(seq))
                return true;
        }

        return false;
    }
}
