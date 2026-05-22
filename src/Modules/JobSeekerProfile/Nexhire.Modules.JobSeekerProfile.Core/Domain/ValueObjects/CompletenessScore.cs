using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class CompletenessScore : ValueObject
{
    public int Percentage { get; }
    public IReadOnlyCollection<string> MissingSections { get; }

    private CompletenessScore(int percentage, IReadOnlyCollection<string> missingSections)
    {
        Percentage = percentage;
        MissingSections = missingSections;
    }

    public static Result<CompletenessScore> Create(int percentage, IEnumerable<string> missingSections)
    {
        if (percentage < 0 || percentage > 100)
        {
            return Result.Failure<CompletenessScore>(new Error("CompletenessScore.InvalidPercentage", "Completeness score must be between 0 and 100."));
        }

        var missingList = missingSections?.ToList() ?? new List<string>();

        return Result.Success(new CompletenessScore(percentage, missingList.AsReadOnly()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Percentage;
        foreach (var section in MissingSections)
        {
            yield return section;
        }
    }
}
