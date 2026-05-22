using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Services;

public sealed class CandidateRankingService
{
    public List<ShortlistCandidate> RankAndFilterCandidates(
        PostingMatchProfile posting,
        List<SeekerMatchProfile> seekers,
        Dictionary<Guid, MatchScore> matchScores,
        QualificationThreshold? threshold,
        HashSet<Guid> directAppliedSeekers,
        Dictionary<Guid, DateTime> seekerLastUpdates)
    {
        var candidates = new List<ShortlistCandidate>();

        foreach (var seeker in seekers)
        {
            bool hasApplied = directAppliedSeekers.Contains(seeker.JobSeekerId);
            
            if (!matchScores.TryGetValue(seeker.JobSeekerId, out var ms))
            {
                continue;
            }

            // Check recruiter threshold filters
            bool passesThreshold = true;
            if (threshold != null && !hasApplied)
            {
                if (ms.OverallScore < threshold.MinOverallMatch)
                {
                    passesThreshold = false;
                }

                var skillScore = ms.Breakdown.Scores.FirstOrDefault(s => s.Factor == MatchFactor.Skill)?.Score ?? 0;
                if (skillScore < threshold.MinSkillMatch)
                {
                    passesThreshold = false;
                }

                if ((int)seeker.EducationLevel < (int)threshold.MinEducationLevel)
                {
                    passesThreshold = false;
                }

                if (seeker.TotalExperienceYears < threshold.MinExperienceYears)
                {
                    passesThreshold = false;
                }

                foreach (var reqCert in threshold.RequiredCertifications)
                {
                    if (!seeker.TrainingCredentials.Contains(reqCert, StringComparer.OrdinalIgnoreCase))
                    {
                        passesThreshold = false;
                        break;
                    }
                }
            }

            if (!passesThreshold && !hasApplied)
            {
                continue;
            }

            // Resolve FitAnalysis details
            var salaryFit = SalaryFitIndicator.Green;
            var salaryScore = ms.Breakdown.Scores.FirstOrDefault(s => s.Factor == MatchFactor.Salary)?.Score ?? 0;
            if (salaryScore < 60) salaryFit = SalaryFitIndicator.Red;
            else if (salaryScore < 85) salaryFit = SalaryFitIndicator.Yellow;

            var timeToProductivity = TimeToProductivityEstimate.TwoToThreeWeeks;
            var skillScoreForProductivity = ms.Breakdown.Scores.FirstOrDefault(s => s.Factor == MatchFactor.Skill)?.Score ?? 0;
            if (skillScoreForProductivity >= 90) timeToProductivity = TimeToProductivityEstimate.OneWeek;
            else if (skillScoreForProductivity < 60) timeToProductivity = TimeToProductivityEstimate.FourPlusWeeks;

            var contactLikelihood = ContactLikelihood.Medium;
            if (ms.OverallScore >= 85) contactLikelihood = ContactLikelihood.High;
            else if (ms.OverallScore < 60) contactLikelihood = ContactLikelihood.Low;

            var workArrangementCompatible = true; // Default match

            var fitAnalysis = FitAnalysis.Create(
                ms.OverallScore,
                ms.Strengths,
                ms.Gaps,
                salaryFit,
                salaryScore,
                motivationScore: ms.OverallScore, // Proxy score
                timeToProductivity,
                contactLikelihood,
                workArrangementCompatible).Value;

            var inclusionReason = hasApplied
                ? ShortlistInclusionReason.AppliedDirectly
                : ShortlistInclusionReason.MatchAboveThreshold;

            var candidate = new ShortlistCandidate(
                seeker.JobSeekerId,
                ms.OverallScore,
                inclusionReason,
                fitAnalysis,
                appliedAtUtc: hasApplied ? DateTime.UtcNow : null); // In integration, actual date will be supplied

            candidates.Add(candidate);
        }

        // Sort: AppliedDirectly first, then by match score descending, break ties by profile updates
        return candidates
            .OrderByDescending(c => c.InclusionReason == ShortlistInclusionReason.AppliedDirectly)
            .ThenByDescending(c => c.OverallMatchScore)
            .ThenByDescending(c => seekerLastUpdates.TryGetValue(c.JobSeekerId, out var dt) ? dt : DateTime.MinValue)
            .ToList();
    }
}
