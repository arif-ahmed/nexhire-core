using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class DateRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime? End { get; }

    private DateRange(DateTime start, DateTime? end)
    {
        Start = start;
        End = end;
    }

    public static Result<DateRange> Create(DateTime start, DateTime? end)
    {
        if (end.HasValue && start > end.Value)
        {
            return Result.Failure<DateRange>(new Error("DateRange.Invalid", "Start date must be less than or equal to end date."));
        }

        return Result.Success(new DateRange(start, end));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        if (End.HasValue)
        {
            yield return End.Value;
        }
    }
}
