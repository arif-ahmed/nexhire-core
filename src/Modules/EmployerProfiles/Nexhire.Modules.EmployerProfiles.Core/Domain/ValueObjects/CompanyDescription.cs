using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public class CompanyDescription : ValueObject
{
    public string Value { get; }

    private CompanyDescription(string value)
    {
        Value = value;
    }

    public static Result<CompanyDescription> Create(string description)
    {
        var val = description ?? string.Empty;
        var trimmed = val.Trim();

        if (trimmed.Length > 5000)
        {
            return Result.Failure<CompanyDescription>(new Error("CompanyDescription.TooLong", "Company description must not exceed 5000 characters."));
        }

        return Result.Success(new CompanyDescription(trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
