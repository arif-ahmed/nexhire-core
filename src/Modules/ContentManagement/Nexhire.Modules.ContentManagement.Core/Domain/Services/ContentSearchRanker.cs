namespace Nexhire.Modules.ContentManagement.Core.Domain.Services;

public record SearchableContent(Guid Id, string Title, string Body, DateTime UpdatedOnUtc, string? TopicOrder = null);

public record ScoredContent(Guid Id, int Score, DateTime UpdatedOnUtc);

public sealed class ContentSearchRanker
{
    public IReadOnlyList<ScoredContent> Rank(string query, IReadOnlyList<SearchableContent> matches)
    {
        var normalizedQuery = query.Trim().ToLowerInvariant();

        var scored = matches.Select(m =>
        {
            var titleMatch = m.Title.ToLowerInvariant().Contains(normalizedQuery);
            var bodyMatch = m.Body.ToLowerInvariant().Contains(normalizedQuery);
            var score = (titleMatch ? 2 : 0) + (bodyMatch ? 1 : 0);
            return new ScoredContent(m.Id, score, m.UpdatedOnUtc);
        })
        .Where(s => s.Score > 0)
        .OrderByDescending(s => s.Score)
        .ThenByDescending(s => s.UpdatedOnUtc)
        .ToList();

        return scored;
    }
}
