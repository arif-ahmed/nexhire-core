using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Services;

public static class ResultRankBlender
{
    public static List<RankedResult> Blend(
        IReadOnlyList<ScoredEntry> relevanceScored,
        IReadOnlyDictionary<Guid, int> matchScoresByPostingId,
        SortOption sort)
    {
        return sort switch
        {
            SortOption.Relevance => relevanceScored
                .OrderByDescending(e => e.Score)
                .ThenByDescending(e => matchScoresByPostingId.GetValueOrDefault(e.EntryId, 0))
                .Select(e => new RankedResult(e.EntryId, e.Score))
                .ToList(),

            SortOption.MatchScore => relevanceScored
                .OrderByDescending(e => matchScoresByPostingId.ContainsKey(e.EntryId))
                .ThenByDescending(e => matchScoresByPostingId.GetValueOrDefault(e.EntryId, 0))
                .Select(e => new RankedResult(e.EntryId, matchScoresByPostingId.GetValueOrDefault(e.EntryId, 0)))
                .ToList(),

            SortOption.DatePosted => relevanceScored
                .OrderByDescending(e => e.PostedOnUtc ?? DateTime.MinValue)
                .Select(e => new RankedResult(e.EntryId, e.Score))
                .ToList(),

            SortOption.Salary => relevanceScored
                .Select(e => e)
                .ToList() // Salary sorting requires data from entries; for now identity order
                .Select(e => new RankedResult(e.EntryId, e.Score))
                .ToList(),

            SortOption.ApplicationDeadline => relevanceScored
                .Select(e => e)
                .ToList()
                .Select(e => new RankedResult(e.EntryId, e.Score))
                .ToList(),

            _ => relevanceScored
                .Select(e => new RankedResult(e.EntryId, e.Score))
                .ToList()
        };
    }
}
