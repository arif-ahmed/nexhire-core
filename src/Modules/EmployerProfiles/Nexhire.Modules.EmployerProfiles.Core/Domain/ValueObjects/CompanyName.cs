using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public class CompanyName : ValueObject
{
    public string Value { get; }

    private CompanyName(string value)
    {
        Value = value;
    }

    public static Result<CompanyName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CompanyName>(new Error("CompanyName.Empty", "Company name cannot be empty."));
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Length > 200)
        {
            return Result.Failure<CompanyName>(new Error("CompanyName.TooLong", "Company name must not exceed 200 characters."));
        }

        return Result.Success(new CompanyName(trimmedValue));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
