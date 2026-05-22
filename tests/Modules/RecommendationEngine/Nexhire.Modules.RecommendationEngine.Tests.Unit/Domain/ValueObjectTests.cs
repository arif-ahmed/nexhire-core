using FluentAssertions;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Tests.Unit.Domain;

public class ValueObjectTests
{
    [Fact]
    public void FactorWeights_Create_ShouldSucceed_WhenSumIsOne()
    {
        // Arrange & Act
        var result = FactorWeights.Create(0.25m, 0.15m, 0.10m, 0.15m, 0.20m, 0.15m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Skill.Should().Be(0.25m);
        result.Value.Education.Should().Be(0.15m);
        result.Value.Training.Should().Be(0.10m);
        result.Value.Location.Should().Be(0.15m);
        result.Value.Experience.Should().Be(0.20m);
        result.Value.Salary.Should().Be(0.15m);
    }

    [Fact]
    public void FactorWeights_Create_ShouldSucceed_WhenSumIsWithinTolerance()
    {
        // Arrange & Act
        // Sum = 0.25 + 0.15 + 0.10 + 0.15 + 0.20 + 0.145 = 0.995 (within 0.01 tolerance of 1.0)
        var result = FactorWeights.Create(0.25m, 0.15m, 0.10m, 0.15m, 0.20m, 0.145m);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FactorWeights_Create_ShouldFail_WhenSumIsOutOfTolerance()
    {
        // Arrange & Act
        // Sum = 0.25 + 0.15 + 0.10 + 0.15 + 0.20 + 0.10 = 0.95 (out of tolerance)
        var result = FactorWeights.Create(0.25m, 0.15m, 0.10m, 0.15m, 0.20m, 0.10m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-WEIGHTS-INVALID-SUM");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void FactorWeights_Create_ShouldFail_WhenAnyWeightIsNegativeOrGreaterThanOne(decimal invalidWeight)
    {
        // Arrange & Act
        var result = FactorWeights.Create(invalidWeight, 0.15m, 0.10m, 0.15m, 0.20m, 0.40m);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void FactorScore_Create_ShouldSucceed_WhenScoreBetweenZeroAndOneHundred()
    {
        // Arrange & Act
        var result = FactorScore.Create(MatchFactor.Skill, 85);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Factor.Should().Be(MatchFactor.Skill);
        result.Value.Score.Should().Be(85);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void FactorScore_Create_ShouldFail_WhenScoreIsOutOfBounds(int invalidScore)
    {
        // Arrange & Act
        var result = FactorScore.Create(MatchFactor.Skill, invalidScore);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-FACTOR-SCORE-OUT-OF-RANGE");
    }

    [Fact]
    public void MatchBreakdown_Create_ShouldSucceed_WhenAllSixFactorsArePresent()
    {
        // Arrange
        var scores = new List<FactorScore>
        {
            FactorScore.Create(MatchFactor.Skill, 80).Value,
            FactorScore.Create(MatchFactor.Education, 70).Value,
            FactorScore.Create(MatchFactor.Training, 90).Value,
            FactorScore.Create(MatchFactor.Location, 60).Value,
            FactorScore.Create(MatchFactor.Experience, 85).Value,
            FactorScore.Create(MatchFactor.Salary, 75).Value
        };

        // Act
        var result = MatchBreakdown.Create(scores);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Scores.Should().HaveCount(6);
    }

    [Fact]
    public void MatchBreakdown_Create_ShouldFail_WhenFactorsAreMissing()
    {
        // Arrange
        var scores = new List<FactorScore>
        {
            FactorScore.Create(MatchFactor.Skill, 80).Value,
            FactorScore.Create(MatchFactor.Education, 70).Value
        };

        // Act
        var result = MatchBreakdown.Create(scores);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-BREAKDOWN-INCOMPLETE");
    }

    [Fact]
    public void MatchBreakdown_Create_ShouldFail_WhenFactorsAreDuplicated()
    {
        // Arrange
        var scores = new List<FactorScore>
        {
            FactorScore.Create(MatchFactor.Skill, 80).Value,
            FactorScore.Create(MatchFactor.Skill, 70).Value, // Duplicate
            FactorScore.Create(MatchFactor.Education, 70).Value,
            FactorScore.Create(MatchFactor.Training, 90).Value,
            FactorScore.Create(MatchFactor.Location, 60).Value,
            FactorScore.Create(MatchFactor.Experience, 85).Value
        };

        // Act
        var result = MatchBreakdown.Create(scores);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-BREAKDOWN-DUPLICATE-FACTOR");
    }

    [Fact]
    public void EmbeddingVector_Create_ShouldSucceed_WhenDimensionMatchesValuesLength()
    {
        // Arrange
        var values = new decimal[768];
        Array.Fill(values, 0.1m);

        // Act
        var result = EmbeddingVector.Create(values.ToList(), 768);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Dimension.Should().Be(768);
    }

    [Fact]
    public void EmbeddingVector_Create_ShouldFail_WhenDimensionMismatchesValuesLength()
    {
        // Arrange
        var values = new List<decimal> { 0.1m, 0.2m };

        // Act
        var result = EmbeddingVector.Create(values, 768);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-EMBEDDING-DIMENSION-MISMATCH");
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void GeoLocation_Create_ShouldFail_WhenLatitudeIsOutOfBounds(decimal invalidLat)
    {
        // Arrange & Act
        var result = GeoLocation.Create(invalidLat, 0.0m, "Cairo");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-LOCATION-LATITUDE-OUT-OF-RANGE");
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void GeoLocation_Create_ShouldFail_WhenLongitudeIsOutOfBounds(decimal invalidLon)
    {
        // Arrange & Act
        var result = GeoLocation.Create(0.0m, invalidLon, "Cairo");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-LOCATION-LONGITUDE-OUT-OF-RANGE");
    }

    [Fact]
    public void SalaryRange_Create_ShouldSucceed_WhenMinIsLessThanOrEqualToMax()
    {
        // Arrange & Act
        var result = SalaryRange.Create(1000m, 2000m, "USD");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SalaryRange_Create_ShouldFail_WhenMinIsGreaterThanMax()
    {
        // Arrange & Act
        var result = SalaryRange.Create(2500m, 2000m, "USD");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-SALARY-MIN-GREATER-THAN-MAX");
    }

    [Fact]
    public void ConfidenceScore_Create_ShouldFlagNeedsReview_WhenScoreIsLessThanSeventy()
    {
        // Arrange & Act
        var result = ConfidenceScore.Create(65);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NeedsReview.Should().BeTrue();
    }

    [Fact]
    public void ConfidenceScore_Create_ShouldNotFlagNeedsReview_WhenScoreIsSeventyOrMore()
    {
        // Arrange & Act
        var result = ConfidenceScore.Create(75);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NeedsReview.Should().BeFalse();
    }
}
