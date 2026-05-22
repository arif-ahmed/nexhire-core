using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

public class SalaryRange : ValueObject
{
    public decimal Min { get; }
    public decimal Max { get; }
    public string Currency { get; }

    private SalaryRange(decimal min, decimal max, string currency)
    {
        Min = min;
        Max = max;
        Currency = currency;
    }

    public static Result<SalaryRange> Create(decimal min, decimal max, string currency = "BDT")
    {
        if (min < 0)
            return Result.Failure<SalaryRange>(new Error("SalaryRange.NegativeMin", "Minimum salary cannot be negative."));

        if (min > max)
            return Result.Failure<SalaryRange>(new Error("SalaryRange.MinExceedsMax", "Minimum salary cannot exceed maximum salary."));

        return Result.Success(new SalaryRange(min, max, currency));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Min;
        yield return Max;
        yield return Currency;
    }
}
