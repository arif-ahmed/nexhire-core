using System.Text.RegularExpressions;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

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

        var trimmed = mobile.Trim();

        if (!E164Regex.IsMatch(trimmed))
        {
            return Result.Failure<MobileNumber>(new Error("MobileNumber.Invalid", "The mobile number format is invalid. Must be in E.164 format (e.g. +8801712345678)."));
        }

        return Result.Success(new MobileNumber(trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
