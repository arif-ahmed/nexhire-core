using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
        {
            return Result.Failure<Money>(new Error("Money.NegativeAmount", "Money amount cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result.Failure<Money>(new Error("Money.EmptyCurrency", "Currency must be provided."));
        }

        return Result.Success(new Money(amount, currency.Trim().ToUpperInvariant()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
