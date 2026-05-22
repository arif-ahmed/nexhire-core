using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Services;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.DomainServices;

public class RelevanceRankerTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly Guid EmployerId = Guid.NewGuid();

    private static JobIndexEntry MakeEntry(
        Guid id, string title, string summary, string[] skills)
    {
        return JobIndexEntry.Project(
            id, EmployerId, title, summary, "Corp", skills,
            null, null, "Dhaka", null, null, null,
            EmploymentType.FullTime, WorkFormat.Remote,
            null, null, null, null,
            Now, null, 1, Now).Value;
    }

    [Fact]
    public void Rank_ShouldScoreTitleHitHighest()
    {
        var weights = RelevanceWeights.Default;
        var entry = MakeEntry(Guid.NewGuid(), "Senior Developer", "A summary", ["SQL"]);

        var results = RelevanceRanker.Rank("developer", null, [entry], weights);

        results.Should().HaveCount(1);
        results[0].Score.Should().Be(weights.TitleWeight);
    }

    [Fact]
    public void Rank_ShouldScoreSkillHitNext()
    {
        var weights = RelevanceWeights.Default;
        var entry = MakeEntry(Guid.NewGuid(), "No Match Title", "A summary", ["C#"]);

        var results = RelevanceRanker.Rank("c#", null, [entry], weights);

        results.Should().HaveCount(1);
        results[0].Score.Should().Be(weights.SkillWeight);
    }

    [Fact]
    public void Rank_ShouldScoreSummaryHitLowest()
    {
        var weights = RelevanceWeights.Default;
        var entry = MakeEntry(Guid.NewGuid(), "No Match", "developer required", ["SQL"]);

        var results = RelevanceRanker.Rank("developer", null, [entry], weights);

        results.Should().HaveCount(1);
        results[0].Score.Should().Be(weights.SummaryWeight);
    }

    [Fact]
    public void Rank_ShouldSumMultipleHits()
    {
        var weights = RelevanceWeights.Default;
        var entry = MakeEntry(Guid.NewGuid(), "Developer", "developer needed", ["developer"]);

        var results = RelevanceRanker.Rank("developer", null, [entry], weights);

        results[0].Score.Should().Be(weights.TitleWeight + weights.SkillWeight + weights.SummaryWeight);
    }

    [Fact]
    public void Rank_ShouldReturnZero_WhenNoMatches()
    {
        var weights = RelevanceWeights.Default;
        var entry = MakeEntry(Guid.NewGuid(), "Manager", "managing things", ["leadership"]);

        var results = RelevanceRanker.Rank("developer", null, [entry], weights);

        results[0].Score.Should().Be(0);
    }

    [Fact]
    public void Rank_ShouldOrderByScoreDescending()
    {
        var weights = RelevanceWeights.Default;
        var highEntry = MakeEntry(Guid.NewGuid(), "Developer", "", []);
        var lowEntry = MakeEntry(Guid.NewGuid(), "Manager", "developer stuff", []);

        var results = RelevanceRanker.Rank("developer", null, [lowEntry, highEntry], weights);

        results[0].EntryId.Should().Be(highEntry.Id);
        results[1].EntryId.Should().Be(lowEntry.Id);
    }

    [Fact]
    public void Rank_ShouldRespectCustomWeights()
    {
        var weights = RelevanceWeights.Create(10.0, 5.0, 1.0).Value;
        var entry = MakeEntry(Guid.NewGuid(), "Developer", "developer", ["developer"]);

        var results = RelevanceRanker.Rank("developer", null, [entry], weights);

        results[0].Score.Should().Be(16.0);
    }
}
