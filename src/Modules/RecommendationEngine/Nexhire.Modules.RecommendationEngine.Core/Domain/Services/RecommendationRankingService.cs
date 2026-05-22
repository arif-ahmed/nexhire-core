using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Services;

public sealed class RecommendationRankingService
{
    public List<RecommendedJob> RankJobs(
        Guid jobSeekerId,
        List<PostingMatchProfile> postings,
        Dictionary<Guid, MatchScore> matchScores,
        List<Guid> collaborativeRecommendations,
        List<Guid> contentRecommendations,
        List<RecommendationFeedback> feedbackLogs)
    {
        var rankedJobs = new List<RecommendedJob>();
        var now = DateTime.UtcNow;

        // Get list of suppressed job posting IDs
        var suppressedPostingIds = feedbackLogs
            .Where(f => f.JobSeekerId == jobSeekerId &&
                        f.Signal == FeedbackSignal.NotInterested &&
                        f.SuppressUntilUtc.HasValue &&
                        f.SuppressUntilUtc.Value > now)
            .Select(f => f.JobPostingId)
            .ToHashSet();

        foreach (var posting in postings)
        {
            if (suppressedPostingIds.Contains(posting.JobPostingId))
            {
                continue;
            }

            // 1. Match Score contribution (0 to 100)
            int matchScoreVal = 0;
            if (matchScores.TryGetValue(posting.JobPostingId, out var ms))
            {
                matchScoreVal = ms.OverallScore;
            }

            // 2. Collaborative score (100 if present, 0 if not)
            decimal collaborativeScore = collaborativeRecommendations.Contains(posting.JobPostingId) ? 100m : 0m;

            // 3. Content score (100 if present, 0 if not)
            decimal contentScore = contentRecommendations.Contains(posting.JobPostingId) ? 100m : 0m;

            // Hybrid calculation: 0.4 * Collaborative + 0.4 * Content + 0.2 * MatchScore
            decimal hybridScore = (0.4m * collaborativeScore) + (0.4m * contentScore) + (0.2m * (decimal)matchScoreVal);

            var topFactors = new List<MatchFactor>();
            if (ms != null)
            {
                topFactors.AddRange(ms.Strengths.Take(3));
            }
            if (topFactors.Count == 0)
            {
                topFactors.Add(MatchFactor.Skill);
            }

            var reason = RecommendationReason.Create(
                $"Recommended based on your strong alignment in {string.Join(", ", topFactors)}.",
                topFactors).Value;

            var recommendedJob = new RecommendedJob(
                posting.JobPostingId,
                matchScoreVal,
                hybridScore,
                reason,
                isSuppressed: false);

            rankedJobs.Add(recommendedJob);
        }

        return rankedJobs
            .OrderByDescending(j => j.HybridScore)
            .ThenByDescending(j => j.MatchScore)
            .ToList();
    }
}
