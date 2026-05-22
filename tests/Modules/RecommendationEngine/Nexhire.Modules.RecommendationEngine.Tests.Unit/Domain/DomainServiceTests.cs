using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Ports;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Services;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Tests.Unit.Domain;

public class DomainServiceTests
{
    private readonly IVectorIndexPort _vectorIndexPort;
    private readonly MatchScoringService _scoringService;
    private readonly CandidatePrivacyFilter _privacyFilter;
    private readonly AbVariantAllocator _variantAllocator;
    private readonly ImpactPreviewCalculator _previewCalculator;

    public DomainServiceTests()
    {
        _vectorIndexPort = Substitute.For<IVectorIndexPort>();
        _scoringService = new MatchScoringService(_vectorIndexPort);
        _privacyFilter = new CandidatePrivacyFilter();
        _variantAllocator = new AbVariantAllocator();
        _previewCalculator = new ImpactPreviewCalculator();
    }

    [Fact]
    public async Task MatchScoringService_ShouldCalculateFactorScoresCorrectly()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();

        // Seeker: Skill C# (prof 4), Bachelors, 5 yrs exp, Boston lat/lon, USD 100k
        var seekerSkill = SkillRequirement.Create("CSHARP", "C#", 4, ConfidenceScore.Create(95).Value).Value;
        var seekerLocation = GeoLocation.Create(42.3601m, -71.0589m, "Boston").Value;
        var seekerSalary = SalaryRange.Create(100000m, 120000m, "USD").Value;
        var seeker = SeekerMatchProfile.Create(
            seekerId,
            new List<SkillRequirement> { seekerSkill },
            EducationLevel.Bachelor,
            new List<string> { "AWS Developer Certificate" },
            5.0m,
            seekerLocation,
            seekerSalary,
            PrivacyLevel.Public);

        // Posting: Skill C# (prof 5), Bachelors, 8 yrs exp, Boston lat/lon, USD 90k-110k
        var postingSkill = SkillRequirement.Create("CSHARP", "C#", 5, ConfidenceScore.Create(90).Value).Value;
        var postingLocation = GeoLocation.Create(42.3601m, -71.0589m, "Boston").Value;
        var postingSalary = SalaryRange.Create(90000m, 110000m, "USD").Value;
        var posting = PostingMatchProfile.Create(
            postingId,
            Guid.NewGuid(),
            new List<SkillRequirement> { postingSkill },
            EducationLevel.Bachelor,
            8.0m,
            postingLocation,
            postingSalary,
            PostingMatchStatus.Active);

        // Act
        var result = await _scoringService.ComputeBreakdownAsync(seeker, posting, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var breakdown = result.Value;

        // Skill: Seeker 4/5 * 100% similarity = 0.8 => 80 score
        var skillScore = breakdown.Scores.First(s => s.Factor == MatchFactor.Skill).Score;
        skillScore.Should().Be(80);

        // Education: Exact match => 100 score
        var eduScore = breakdown.Scores.First(s => s.Factor == MatchFactor.Education).Score;
        eduScore.Should().Be(100);

        // Experience: 5 / 8 = 62.5% => 63 score
        var expScore = breakdown.Scores.First(s => s.Factor == MatchFactor.Experience).Score;
        expScore.Should().Be(63);

        // Location: same city => 100 score
        var locScore = breakdown.Scores.First(s => s.Factor == MatchFactor.Location).Score;
        locScore.Should().Be(100);

        // Salary: Seeker Min 100k is within posting range 90k-110k => 100 score
        var salScore = breakdown.Scores.First(s => s.Factor == MatchFactor.Salary).Score;
        salScore.Should().Be(100);
    }

    [Fact]
    public async Task MatchScoringService_SkillSimilarity_ShouldUtilizeVectorSimilarityThreshold()
    {
        // Arrange
        var seekerSkill = SkillRequirement.Create("JAVA", "Java", 5, ConfidenceScore.Create(95).Value).Value;
        var seeker = SeekerMatchProfile.Create(Guid.NewGuid(), new List<SkillRequirement> { seekerSkill }, EducationLevel.None, null, 0, null, null, PrivacyLevel.Public);

        var postingSkill = SkillRequirement.Create("KOTLIN", "Kotlin", 5, ConfidenceScore.Create(95).Value).Value;
        var posting = PostingMatchProfile.Create(Guid.NewGuid(), Guid.NewGuid(), new List<SkillRequirement> { postingSkill }, EducationLevel.None, 0, null, null, PostingMatchStatus.Active);

        // Setup similarity = 0.80 (above 0.75 threshold)
        _vectorIndexPort.GetSkillSimilarityAsync("JAVA", "KOTLIN", Arg.Any<CancellationToken>()).Returns(0.80m);

        // Act
        var result = await _scoringService.ComputeBreakdownAsync(seeker, posting, CancellationToken.None);

        // Assert
        var skillScore = result.Value.Scores.First(s => s.Factor == MatchFactor.Skill).Score;
        skillScore.Should().Be(80); // 0.80 * 100
    }

    [Fact]
    public async Task MatchScoringService_SkillSimilarity_ShouldRejectLowSimilarity()
    {
        // Arrange
        var seekerSkill = SkillRequirement.Create("JAVA", "Java", 5, ConfidenceScore.Create(95).Value).Value;
        var seeker = SeekerMatchProfile.Create(Guid.NewGuid(), new List<SkillRequirement> { seekerSkill }, EducationLevel.None, null, 0, null, null, PrivacyLevel.Public);

        var postingSkill = SkillRequirement.Create("PYTHON", "Python", 5, ConfidenceScore.Create(95).Value).Value;
        var posting = PostingMatchProfile.Create(Guid.NewGuid(), Guid.NewGuid(), new List<SkillRequirement> { postingSkill }, EducationLevel.None, 0, null, null, PostingMatchStatus.Active);

        // Setup similarity = 0.60 (below 0.75 threshold)
        _vectorIndexPort.GetSkillSimilarityAsync("JAVA", "PYTHON", Arg.Any<CancellationToken>()).Returns(0.60m);

        // Act
        var result = await _scoringService.ComputeBreakdownAsync(seeker, posting, CancellationToken.None);

        // Assert
        var skillScore = result.Value.Scores.First(s => s.Factor == MatchFactor.Skill).Score;
        skillScore.Should().Be(0); // Under threshold gets 0
    }

    [Theory]
    [InlineData(PrivacyLevel.Public, false, true)]
    [InlineData(PrivacyLevel.ApplyOnly, false, false)]
    [InlineData(PrivacyLevel.ApplyOnly, true, true)]
    [InlineData(PrivacyLevel.Hidden, false, false)]
    [InlineData(PrivacyLevel.Hidden, true, true)]
    public void CandidatePrivacyFilter_ShouldEnforcePrivacyRules(PrivacyLevel privacy, bool hasApplied, bool expectedVisible)
    {
        // Arrange
        var seeker = SeekerMatchProfile.Create(Guid.NewGuid(), new List<SkillRequirement>(), EducationLevel.None, null, 0, null, null, privacy);
        var posting = PostingMatchProfile.Create(Guid.NewGuid(), Guid.NewGuid(), new List<SkillRequirement>(), EducationLevel.None, 0, null, null, PostingMatchStatus.Active);

        // Act
        var visible = _privacyFilter.IsVisible(seeker, posting, hasApplied, "Shortlist", out var exposureEvent);

        // Assert
        visible.Should().Be(expectedVisible);
        if (expectedVisible)
        {
            exposureEvent.Should().NotBeNull();
            exposureEvent!.JobSeekerId.Should().Be(seeker.JobSeekerId);
            exposureEvent.JobPostingId.Should().Be(posting.JobPostingId);
            exposureEvent.ExposureContext.Should().Be("Shortlist");
        }
        else
        {
            exposureEvent.Should().BeNull();
        }
    }

    [Fact]
    public void AbVariantAllocator_ShouldDeterministicallyAllocateVariant()
    {
        // Arrange
        var weightsControl = FactorWeights.Create(0.25m, 0.15m, 0.10m, 0.15m, 0.20m, 0.15m).Value;
        var profileControl = MatchingWeightProfile.Create("v1", weightsControl, "control", 80, Guid.NewGuid());
        profileControl.Activate();

        var weightsExpr = FactorWeights.Create(0.30m, 0.10m, 0.10m, 0.15m, 0.20m, 0.15m).Value;
        var profileExpr = MatchingWeightProfile.Create("v1-experimental", weightsExpr, "experimental", 20, Guid.NewGuid());
        profileExpr.Activate();

        var activeProfiles = new List<MatchingWeightProfile> { profileControl, profileExpr };
        var seekerId = Guid.NewGuid();

        // Act
        var allocated1 = _variantAllocator.AllocateVariant(seekerId, activeProfiles);
        var allocated2 = _variantAllocator.AllocateVariant(seekerId, activeProfiles);

        // Assert
        allocated1.Should().NotBeNull();
        allocated2.Version.Should().Be(allocated1.Version); // Stable/Deterministic!
    }

    [Fact]
    public void ImpactPreviewCalculator_ShouldComputeAccuratePreviewMetrics()
    {
        // Arrange
        var weights = FactorWeights.Create(0.25m, 0.15m, 0.10m, 0.15m, 0.20m, 0.15m).Value;
        var profile = MatchingWeightProfile.CreateInitial();

        // Create sample match scores: 55, 65, 75, 85
        var ms1 = MatchScore.Compute(Guid.NewGuid(), Guid.NewGuid(), profile, CreateMockBreakdown(55));
        var ms2 = MatchScore.Compute(Guid.NewGuid(), Guid.NewGuid(), profile, CreateMockBreakdown(65));
        var ms3 = MatchScore.Compute(Guid.NewGuid(), Guid.NewGuid(), profile, CreateMockBreakdown(75));
        var ms4 = MatchScore.Compute(Guid.NewGuid(), Guid.NewGuid(), profile, CreateMockBreakdown(85));

        var sample = new List<MatchScore> { ms1, ms2, ms3, ms4 };

        // Act
        // Current threshold: 60, Proposed: 70
        var preview = _previewCalculator.PreviewImpact(60, 70, sample);

        // Assert
        preview.TotalSamplesAnalyzed.Should().Be(4);
        preview.MatchesAboveCurrentThreshold.Should().Be(3); // 65, 75, 85
        preview.MatchesAboveProposedThreshold.Should().Be(2);  // 75, 85
        preview.CandidatesExcludedCount.Should().Be(1);       // 65 is excluded
        preview.CandidatesNewlyIncludedCount.Should().Be(0);   // none newly included
        preview.ShortlistPercentChange.Should().Be(-33.33333333333333333333333333m); // -1/3 * 100
    }

    private MatchBreakdown CreateMockBreakdown(int score)
    {
        var list = new List<FactorScore>
        {
            FactorScore.Create(MatchFactor.Skill, score).Value,
            FactorScore.Create(MatchFactor.Education, score).Value,
            FactorScore.Create(MatchFactor.Training, score).Value,
            FactorScore.Create(MatchFactor.Location, score).Value,
            FactorScore.Create(MatchFactor.Experience, score).Value,
            FactorScore.Create(MatchFactor.Salary, score).Value
        };
        return MatchBreakdown.Create(list).Value;
    }
}
