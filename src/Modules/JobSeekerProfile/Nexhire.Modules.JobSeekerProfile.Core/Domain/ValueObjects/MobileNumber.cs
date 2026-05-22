using System.Text.RegularExpressions;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class MobileNumber : ValueObject
{
    private static readonly Regex E164Regex = new(
        @"^\+[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    public string Value { get; }

    private MobileNumber(string value)
    {
        Value = value;
    }

    public static Result<MobileNumber> Create(string mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            return Result.Failure<MobileNumber>(new Error("MobileNumber.Empty", "Mobile number cannot be empty."));
        }

        var formatted = mobile.Trim().Replace(" ", "").Replace("-", "");

        // Format to E.164 with +880 default region for Bangladesh
        if (formatted.StartsWith("0"))
        {
            formatted = "+880" + formatted.Substring(1);
        }
        else if (formatted.Length == 10 && formatted.StartsWith("1"))
        {
            formatted = "+880" + formatted;
        }

        if (!E164Regex.IsMatch(formatted))
        {
            return Result.Failure<MobileNumber>(new Error("MobileNumber.Invalid", "The mobile number format is invalid."));
        }

        return Result.Success(new MobileNumber(formatted));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
