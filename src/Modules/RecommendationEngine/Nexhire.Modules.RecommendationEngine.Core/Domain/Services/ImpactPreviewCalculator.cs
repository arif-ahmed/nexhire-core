using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Services;

public sealed class ImpactPreviewCalculator
{
    public ThresholdImpactPreview PreviewImpact(
        int currentThreshold,
        int proposedThreshold,
        List<MatchScore> sampleMatchScores)
    {
        if (sampleMatchScores == null || sampleMatchScores.Count == 0)
        {
            return new ThresholdImpactPreview(0, 0, 0, 0, 0, 0.0m);
        }

        int totalCount = sampleMatchScores.Count;
        int activeBefore = sampleMatchScores.Count(s => s.OverallScore >= currentThreshold);
        int activeAfter = sampleMatchScores.Count(s => s.OverallScore >= proposedThreshold);

        int excludedCount = sampleMatchScores.Count(s => s.OverallScore >= currentThreshold && s.OverallScore < proposedThreshold);
        int newlyIncludedCount = sampleMatchScores.Count(s => s.OverallScore < currentThreshold && s.OverallScore >= proposedThreshold);

        decimal percentChange = 0.0m;
        if (activeBefore > 0)
        {
            percentChange = ((decimal)(activeAfter - activeBefore) / activeBefore) * 100m;
        }

        return new ThresholdImpactPreview(
            totalCount,
            activeBefore,
            activeAfter,
            excludedCount,
            newlyIncludedCount,
            percentChange);
    }
}

public sealed record ThresholdImpactPreview(
    int TotalSamplesAnalyzed,
    int MatchesAboveCurrentThreshold,
    int MatchesAboveProposedThreshold,
    int CandidatesExcludedCount,
    int CandidatesNewlyIncludedCount,
    decimal ShortlistPercentChange);
