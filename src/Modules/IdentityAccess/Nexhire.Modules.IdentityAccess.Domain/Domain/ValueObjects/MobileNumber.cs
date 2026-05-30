using System.Text.RegularExpressions;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

public class MobileNumber : ValueObject
{
    public string Value { get; }

    private MobileNumber(string value)
    {
        Value = value;
    }

    public static Result<MobileNumber> Create(string mobile, string defaultCountryCode = "+880")
    {
        if (string.IsNullOrWhiteSpace(mobile))
            return Result.Failure<MobileNumber>(new Error("Mobile.Empty", "Mobile number cannot be empty."));

        var trimmedMobile = mobile.Trim();
        var normalizedMobile = trimmedMobile;

        if (!normalizedMobile.StartsWith("+"))
        {
            if (normalizedMobile.StartsWith(defaultCountryCode.TrimStart('+')))
            {
                normalizedMobile = "+" + normalizedMobile;
            }
            else if (normalizedMobile.StartsWith("0"))
            {
                normalizedMobile = defaultCountryCode + normalizedMobile.Substring(1);
            }
            else
            {
                normalizedMobile = defaultCountryCode + normalizedMobile;
            }
        }

        var e164Regex = new Regex(@"^\+[1-9]\d{9,14}$");

        if (!e164Regex.IsMatch(normalizedMobile))
            return Result.Failure<MobileNumber>(new Error("Mobile.Invalid", "Mobile number must be a valid E.164 number."));

        return new MobileNumber(normalizedMobile);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
