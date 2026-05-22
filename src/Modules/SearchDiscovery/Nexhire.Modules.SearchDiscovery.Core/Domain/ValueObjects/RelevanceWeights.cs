using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

public class RelevanceWeights : ValueObject
{
    public double TitleWeight { get; }
    public double SkillWeight { get; }
    public double SummaryWeight { get; }

    private RelevanceWeights(double titleWeight, double skillWeight, double summaryWeight)
    {
        TitleWeight = titleWeight;
        SkillWeight = skillWeight;
        SummaryWeight = summaryWeight;
    }

    public static Result<RelevanceWeights> Create(double titleWeight, double skillWeight, double summaryWeight)
    {
        if (titleWeight <= 0 || skillWeight <= 0 || summaryWeight <= 0)
            return Result.Failure<RelevanceWeights>(new Error("RelevanceWeights.NonPositiveWeight", "All weights must be positive."));

        if (titleWeight <= skillWeight || skillWeight <= summaryWeight)
            return Result.Failure<RelevanceWeights>(new Error("RelevanceWeights.InvalidOrdering", "Title weight must be greatest, followed by skill, then summary."));

        return Result.Success(new RelevanceWeights(titleWeight, skillWeight, summaryWeight));
    }

    public static RelevanceWeights Default { get; } = new(3.0, 2.0, 1.0);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return TitleWeight;
        yield return SkillWeight;
        yield return SummaryWeight;
    }
}
