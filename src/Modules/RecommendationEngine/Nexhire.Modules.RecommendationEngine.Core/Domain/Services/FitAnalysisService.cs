using System;
using System.Linq;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Services;

public sealed class FitAnalysisService
{
    public FitAnalysis AnalyzeFit(
        SeekerMatchProfile seeker,
        PostingMatchProfile posting,
        MatchScore matchScore)
    {
        var salaryFit = SalaryFitIndicator.Green;
        var salaryScore = matchScore.Breakdown.Scores.FirstOrDefault(s => s.Factor == MatchFactor.Salary)?.Score ?? 0;
        if (salaryScore < 60) salaryFit = SalaryFitIndicator.Red;
        else if (salaryScore < 85) salaryFit = SalaryFitIndicator.Yellow;

        var timeToProductivity = TimeToProductivityEstimate.TwoToThreeWeeks;
        var skillScore = matchScore.Breakdown.Scores.FirstOrDefault(s => s.Factor == MatchFactor.Skill)?.Score ?? 0;
        if (skillScore >= 90) timeToProductivity = TimeToProductivityEstimate.OneWeek;
        else if (skillScore < 60) timeToProductivity = TimeToProductivityEstimate.FourPlusWeeks;

        var contactLikelihood = ContactLikelihood.Medium;
        if (matchScore.OverallScore >= 85) contactLikelihood = ContactLikelihood.High;
        else if (matchScore.OverallScore < 60) contactLikelihood = ContactLikelihood.Low;

        var workArrangementCompatible = true; // In a complex setup, we would compare arrangement preferences.

        return FitAnalysis.Create(
            matchScore.OverallScore,
            matchScore.Strengths,
            matchScore.Gaps,
            salaryFit,
            salaryScore,
            motivationScore: matchScore.OverallScore,
            timeToProductivity,
            contactLikelihood,
            workArrangementCompatible).Value;
    }
}
