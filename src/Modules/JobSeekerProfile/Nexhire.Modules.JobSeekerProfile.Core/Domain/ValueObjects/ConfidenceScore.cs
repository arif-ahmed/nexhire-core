using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class ConfidenceScore : ValueObject
{
    public int Value { get; }
    public bool NeedsVerification => Value < 70;

    private ConfidenceScore(int value)
    {
        Value = value;
    }

    public static Result<ConfidenceScore> Create(int value)
    {
        if (value < 0 || value > 100)
        {
            return Result.Failure<ConfidenceScore>(new Error("ConfidenceScore.InvalidValue", "Confidence score must be between 0 and 100."));
        }

        return Result.Success(new ConfidenceScore(value));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
