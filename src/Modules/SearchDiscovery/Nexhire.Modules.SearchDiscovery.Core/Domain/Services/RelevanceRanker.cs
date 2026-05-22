using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Services;

public static class RelevanceRanker
{
    public static List<ScoredEntry> Rank(
        string? keyword,
        IntentHint? intentHint,
        IReadOnlyList<JobIndexEntry> entries,
        RelevanceWeights weights)
    {
        var normalizedKeyword = keyword?.Trim().ToLowerInvariant();
        var hintSkills = intentHint?.SkillTerms.Select(s => s.ToLowerInvariant()).ToHashSet();

        var scored = entries.Select(entry =>
        {
            var score = 0.0;

            if (!string.IsNullOrEmpty(normalizedKeyword))
            {
                if (entry.Title.ToLowerInvariant().Contains(normalizedKeyword))
                    score += weights.TitleWeight;

                if (entry.Skills.Any(s => s.ToLowerInvariant().Contains(normalizedKeyword)))
                    score += weights.SkillWeight;

                if (entry.Summary.ToLowerInvariant().Contains(normalizedKeyword))
                    score += weights.SummaryWeight;
            }

            if (hintSkills is { Count: > 0 })
            {
                var overlap = entry.Skills.Count(s => hintSkills.Contains(s.ToLowerInvariant()));
                if (overlap > 0)
                    score += weights.SkillWeight * ((double)overlap / hintSkills.Count);
            }

            return new ScoredEntry(entry.Id, score, entry.PostedOnUtc);
        }).ToList();

        return scored.OrderByDescending(e => e.Score).ToList();
    }
}
