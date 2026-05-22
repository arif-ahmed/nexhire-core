using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Services;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.DomainServices;

public class ResultRankBlenderTests
{
    [Fact]
    public void Blend_RelevanceSort_ShouldOrderByRelevanceWithMatchScoreTiebreaker()
    {
        var scored = new List<ScoredEntry>
        {
            new(Guid.NewGuid(), 5.0),
            new(Guid.NewGuid(), 10.0),
            new(Guid.NewGuid(), 10.0)
        };
        var matchScores = new Dictionary<Guid, int>
        {
            { scored[1].EntryId, 30 },
            { scored[2].EntryId, 50 }
        };

        var results = ResultRankBlender.Blend(scored, matchScores, SortOption.Relevance);

        results[0].EntryId.Should().Be(scored[2].EntryId); // same relevance, higher match
        results[1].EntryId.Should().Be(scored[1].EntryId);
        results[2].EntryId.Should().Be(scored[0].EntryId);
    }

    [Fact]
    public void Blend_MatchScoreSort_ShouldOrderByMatchScoreDescending()
    {
        var scored = new List<ScoredEntry>
        {
            new(Guid.NewGuid(), 0),
            new(Guid.NewGuid(), 0)
        };
        var matchScores = new Dictionary<Guid, int>
        {
            { scored[0].EntryId, 80 },
            { scored[1].EntryId, 40 }
        };

        var results = ResultRankBlender.Blend(scored, matchScores, SortOption.MatchScore);

        results[0].EntryId.Should().Be(scored[0].EntryId);
        results[1].EntryId.Should().Be(scored[1].EntryId);
    }

    [Fact]
    public void Blend_MatchScoreSort_ShouldPutUnscoredLast()
    {
        var scored = new List<ScoredEntry>
        {
            new(Guid.NewGuid(), 0), // no match score
            new(Guid.NewGuid(), 0)
        };
        var matchScores = new Dictionary<Guid, int>
        {
            { scored[1].EntryId, 40 }
        };

        var results = ResultRankBlender.Blend(scored, matchScores, SortOption.MatchScore);

        results.Last().EntryId.Should().Be(scored[0].EntryId); // unscored at end
    }

    [Fact]
    public void Blend_ShouldNeverDropResults_WhenMatchScoreMissing()
    {
        var scored = new List<ScoredEntry>
        {
            new(Guid.NewGuid(), 5.0),
            new(Guid.NewGuid(), 3.0),
            new(Guid.NewGuid(), 1.0)
        };
        var matchScores = new Dictionary<Guid, int>
        {
            { scored[0].EntryId, 90 }
        };

        var results = ResultRankBlender.Blend(scored, matchScores, SortOption.MatchScore);

        results.Should().HaveCount(3); // none dropped
    }

    [Fact]
    public void Blend_DatePostedSort_ShouldOrderByDate()
    {
        var now = DateTime.UtcNow;
        var scored = new List<ScoredEntry>
        {
            new(Guid.NewGuid(), 0, now.AddDays(-2)),
            new(Guid.NewGuid(), 0, now),
            new(Guid.NewGuid(), 0, now.AddDays(-1))
        };

        var results = ResultRankBlender.Blend(scored, new Dictionary<Guid, int>(), SortOption.DatePosted);

        results[0].EntryId.Should().Be(scored[1].EntryId); // newest first
    }
}
