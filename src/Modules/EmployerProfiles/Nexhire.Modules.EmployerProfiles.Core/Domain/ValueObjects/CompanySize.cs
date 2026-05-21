using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public enum CompanySizeEnum
{
    Micro,
    Small,
    Medium,
    Large
}

public class CompanySize : ValueObject
{
    public CompanySizeEnum Value { get; }

    private CompanySize(CompanySizeEnum value)
    {
        Value = value;
    }

    public static Result<CompanySize> Create(string size)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return Result.Failure<CompanySize>(new Error("CompanySize.Empty", "Company size cannot be empty."));
        }

        if (!Enum.TryParse<CompanySizeEnum>(size, true, out var sizeEnum))
        {
            return Result.Failure<CompanySize>(new Error("CompanySize.Invalid", "Company size is invalid. Must be Micro, Small, Medium, or Large."));
        }

        return Result.Success(new CompanySize(sizeEnum));
    }

    public static Result<CompanySize> Create(CompanySizeEnum size)
    {
        return Result.Success(new CompanySize(size));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
