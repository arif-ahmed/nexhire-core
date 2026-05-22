using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.ValueObjects;

public class RelevanceWeightsTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenTitleGreaterSkillGreaterSummary()
    {
        var result = RelevanceWeights.Create(3.0, 2.0, 1.0);

        result.IsSuccess.Should().BeTrue();
        result.Value.TitleWeight.Should().Be(3.0);
        result.Value.SkillWeight.Should().Be(2.0);
        result.Value.SummaryWeight.Should().Be(1.0);
    }

    [Fact]
    public void Create_ShouldFail_WhenTitleWeightNotGreatest()
    {
        var result = RelevanceWeights.Create(1.0, 2.0, 1.0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RelevanceWeights.InvalidOrdering");
    }

    [Fact]
    public void Create_ShouldFail_WhenSkillWeightNotGreaterThanSummary()
    {
        var result = RelevanceWeights.Create(3.0, 1.0, 2.0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RelevanceWeights.InvalidOrdering");
    }

    [Fact]
    public void Create_ShouldFail_WhenAnyWeightZero()
    {
        var result = RelevanceWeights.Create(0, 2.0, 1.0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RelevanceWeights.NonPositiveWeight");
    }

    [Fact]
    public void Create_ShouldProvideDefaultWeights()
    {
        var weights = RelevanceWeights.Default;

        weights.TitleWeight.Should().BeGreaterThan(weights.SkillWeight);
        weights.SkillWeight.Should().BeGreaterThan(weights.SummaryWeight);
    }
}
