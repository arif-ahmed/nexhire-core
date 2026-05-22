using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class SalaryExpectation : ValueObject
{
    public Money Min { get; }
    public Money Max { get; }

    private SalaryExpectation(Money min, Money max)
    {
        Min = min;
        Max = max;
    }

    public static Result<SalaryExpectation> Create(Money min, Money max)
    {
        if (min == null || max == null)
        {
            return Result.Failure<SalaryExpectation>(new Error("SalaryExpectation.NullValues", "Min and Max salary expectation must not be null."));
        }

        if (min.Currency != max.Currency)
        {
            return Result.Failure<SalaryExpectation>(new Error("SalaryExpectation.CurrencyMismatch", "Currencies must match."));
        }

        if (min.Amount > max.Amount)
        {
            return Result.Failure<SalaryExpectation>(new Error("SalaryExpectation.MinGreaterThanMax", "Minimum salary cannot exceed maximum salary."));
        }

        return Result.Success(new SalaryExpectation(min, max));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Min;
        yield return Max;
    }
}
