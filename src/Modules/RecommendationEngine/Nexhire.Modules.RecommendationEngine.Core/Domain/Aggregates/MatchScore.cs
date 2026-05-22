using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

public sealed class MatchScore : AggregateRoot<MatchScoreId>
{
    public Guid JobSeekerId { get; private set; }
    public Guid JobPostingId { get; private set; }
    public int OverallScore { get; private set; }
    public MatchBreakdown Breakdown { get; private set; } = null!;
    public string WeightProfileVersion { get; private set; } = null!;
    public string WeightVariantId { get; private set; } = null!;
    public List<MatchFactor> Strengths { get; private set; } = new();
    public List<MatchFactor> Gaps { get; private set; } = new();
    public bool IsStale { get; private set; }

    private MatchScore() : base(MatchScoreId.New()) { }

    private MatchScore(
        MatchScoreId id,
        Guid jobSeekerId,
        Guid jobPostingId,
        int overallScore,
        MatchBreakdown breakdown,
        string weightProfileVersion,
        string weightVariantId,
        List<MatchFactor> strengths,
        List<MatchFactor> gaps,
        bool isStale) : base(id)
    {
        JobSeekerId = jobSeekerId;
        JobPostingId = jobPostingId;
        OverallScore = overallScore;
        Breakdown = breakdown;
        WeightProfileVersion = weightProfileVersion;
        WeightVariantId = weightVariantId;
        Strengths = strengths;
        Gaps = gaps;
        IsStale = isStale;
    }

    public static MatchScore Compute(
        Guid jobSeekerId,
        Guid jobPostingId,
        MatchingWeightProfile weightProfile,
        MatchBreakdown breakdown)
    {
        var (overallScore, strengths, gaps) = CalculateMetrics(breakdown, weightProfile.Weights);

        return new MatchScore(
            MatchScoreId.New(),
            jobSeekerId,
            jobPostingId,
            overallScore,
            breakdown,
            weightProfile.Version,
            weightProfile.VariantId,
            strengths,
            gaps,
            isStale: false);
    }

    public void Recompute(MatchingWeightProfile weightProfile, MatchBreakdown breakdown)
    {
        var (overallScore, strengths, gaps) = CalculateMetrics(breakdown, weightProfile.Weights);

        OverallScore = overallScore;
        Breakdown = breakdown;
        WeightProfileVersion = weightProfile.Version;
        WeightVariantId = weightProfile.VariantId;
        Strengths = strengths;
        Gaps = gaps;
        IsStale = false;
    }

    public void MarkStale()
    {
        IsStale = true;
    }

    private static (int OverallScore, List<MatchFactor> Strengths, List<MatchFactor> Gaps) CalculateMetrics(
        MatchBreakdown breakdown,
        FactorWeights weights)
    {
        decimal weightedSum = 0;
        var strengths = new List<MatchFactor>();
        var gaps = new List<MatchFactor>();

        foreach (var factorScore in breakdown.Scores)
        {
            decimal weight = factorScore.Factor switch
            {
                MatchFactor.Skill => weights.Skill,
                MatchFactor.Education => weights.Education,
                MatchFactor.Training => weights.Training,
                MatchFactor.Location => weights.Location,
                MatchFactor.Experience => weights.Experience,
                MatchFactor.Salary => weights.Salary,
                _ => 0m
            };

            weightedSum += factorScore.Score * weight;

            if (factorScore.Score > 80)
            {
                strengths.Add(factorScore.Factor);
            }
            else if (factorScore.Score < 60)
            {
                gaps.Add(factorScore.Factor);
            }
        }

        int overallScore = (int)Math.Round(weightedSum, MidpointRounding.AwayFromZero);
        return (overallScore, strengths, gaps);
    }
}
