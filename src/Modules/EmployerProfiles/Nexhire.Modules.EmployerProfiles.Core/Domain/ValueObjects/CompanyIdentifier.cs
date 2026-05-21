using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public class CompanyIdentifier : ValueObject
{
    public string Value { get; }

    private CompanyIdentifier(string value)
    {
        Value = value;
    }

    public static Result<CompanyIdentifier> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CompanyIdentifier>(new Error("CompanyIdentifier.Empty", "Company identifier cannot be empty."));
        }

        return Result.Success(new CompanyIdentifier(value.Trim()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
