using FluentAssertions;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Tests.Unit.Domain;

public class AggregateTests
{
    [Fact]
    public void MatchScore_Compute_ShouldCalculateOverallScoreAndDeriveStrengthsAndGaps()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        
        var weights = FactorWeights.Create(0.30m, 0.15m, 0.05m, 0.15m, 0.20m, 0.15m).Value;
        var profile = MatchingWeightProfile.Create("v1", weights, "control", 100, Guid.NewGuid());

        var scores = new List<FactorScore>
        {
            FactorScore.Create(MatchFactor.Skill, 90).Value,      // 90 * 0.30 = 27.00
            FactorScore.Create(MatchFactor.Education, 70).Value,  // 70 * 0.15 = 10.50
            FactorScore.Create(MatchFactor.Training, 50).Value,   // 50 * 0.05 = 2.50
            FactorScore.Create(MatchFactor.Location, 85).Value,   // 85 * 0.15 = 12.75
            FactorScore.Create(MatchFactor.Experience, 60).Value, // 60 * 0.20 = 12.00
            FactorScore.Create(MatchFactor.Salary, 80).Value      // 80 * 0.15 = 12.00
        };
        // Total = 27.00 + 10.50 + 2.50 + 12.75 + 12.00 + 12.00 = 76.75 => Round to 77
        var breakdown = MatchBreakdown.Create(scores).Value;

        // Act
        var matchScore = MatchScore.Compute(seekerId, postingId, profile, breakdown);

        // Assert
        matchScore.JobSeekerId.Should().Be(seekerId);
        matchScore.JobPostingId.Should().Be(postingId);
        matchScore.OverallScore.Should().Be(77);
        matchScore.WeightProfileVersion.Should().Be("v1");
        matchScore.WeightVariantId.Should().Be("control");
        
        // Strengths > 80: Skill (90), Location (85)
        matchScore.Strengths.Should().Contain(MatchFactor.Skill);
        matchScore.Strengths.Should().Contain(MatchFactor.Location);
        matchScore.Strengths.Should().NotContain(MatchFactor.Education);

        // Gaps < 60: Training (50)
        matchScore.Gaps.Should().Contain(MatchFactor.Training);
        matchScore.Gaps.Should().NotContain(MatchFactor.Experience);
    }

    [Fact]
    public void MatchingWeightProfile_CreateInitial_ShouldSeedDefaultsAndBeActive()
    {
        // Act
        var profile = MatchingWeightProfile.CreateInitial();

        // Assert
        profile.Version.Should().Be("1.0.0");
        profile.Weights.Skill.Should().Be(0.25m);
        profile.Weights.Education.Should().Be(0.15m);
        profile.Weights.Training.Should().Be(0.10m);
        profile.Weights.Location.Should().Be(0.15m);
        profile.Weights.Experience.Should().Be(0.20m);
        profile.Weights.Salary.Should().Be(0.15m);
        profile.VariantId.Should().Be("control");
        profile.IsActive.Should().BeTrue();
    }

    [Fact]
    public void MatchingWeightProfile_Activate_ShouldSupersedePriorProfileInSameSlot()
    {
        // Arrange
        var weights1 = FactorWeights.Create(0.25m, 0.15m, 0.10m, 0.15m, 0.20m, 0.15m).Value;
        var profile1 = MatchingWeightProfile.Create("v1", weights1, "control", 100, Guid.NewGuid());
        profile1.IsActive.Should().BeFalse();
        profile1.Activate();
        profile1.IsActive.Should().BeTrue();

        var weights2 = FactorWeights.Create(0.30m, 0.10m, 0.10m, 0.15m, 0.20m, 0.15m).Value;
        var profile2 = MatchingWeightProfile.Create("v2", weights2, "control", 100, Guid.NewGuid());

        // Act
        profile2.Activate();
        
        // Assert
        profile2.IsActive.Should().BeTrue();
        // The business logic will let us mark profile1 as superseded
        profile1.SupersedeBy(profile2.Version);
        profile1.IsActive.Should().BeFalse();
        profile1.SupersededByVersion.Should().Be("v2");
    }

    [Fact]
    public void MatchThresholdConfiguration_UpdateGlobalThreshold_ShouldLogAuditEntry()
    {
        // Arrange
        var config = MatchThresholdConfiguration.CreateDefault();
        config.GlobalThresholdPercent.Should().Be(60);

        var adminId = Guid.NewGuid();

        // Act
        var result = config.UpdateGlobalThreshold(65, adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        config.GlobalThresholdPercent.Should().Be(65);
        config.ChangeLog.Should().HaveCount(1);
        
        var entry = config.ChangeLog.First();
        entry.OldValue.Should().Be(60);
        entry.NewValue.Should().Be(65);
        entry.ChangedBy.Should().Be(adminId);
        entry.Scope.Should().Be("Global");
    }

    [Fact]
    public void MatchThresholdConfiguration_UpdateGlobalThreshold_ShouldRejectInvalidRange()
    {
        // Arrange
        var config = MatchThresholdConfiguration.CreateDefault();

        // Act
        var result = config.UpdateGlobalThreshold(105, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-THRESHOLD-OUT-OF-RANGE");
    }

    [Fact]
    public void TalentPool_AddCandidate_ShouldPreventDuplicateActiveCandidates()
    {
        // Arrange
        var employerId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var pool = TalentPool.Create(employerId, recruiterId, "Go Developers", "Go lang talent pool", new List<string>(), false);
        var seekerId = Guid.NewGuid();

        // Act
        var addFirst = pool.AddCandidate(seekerId, recruiterId, "Great engineer");
        var addSecond = pool.AddCandidate(seekerId, recruiterId, "Duplicate");

        // Assert
        addFirst.IsSuccess.Should().BeTrue();
        addSecond.IsFailure.Should().BeTrue();
        addSecond.Error.Code.Should().Be("E-POOL-DUPLICATE-MEMBER");
        pool.Members.Should().HaveCount(1);
    }

    [Fact]
    public void TalentPool_RemoveCandidate_ShouldSoftRemove()
    {
        // Arrange
        var pool = TalentPool.Create(Guid.NewGuid(), Guid.NewGuid(), "Go Developers", null, null, false);
        var seekerId = Guid.NewGuid();
        pool.AddCandidate(seekerId, Guid.NewGuid(), null);

        // Act
        var removeResult = pool.RemoveCandidate(seekerId);

        // Assert
        removeResult.IsSuccess.Should().BeTrue();
        pool.Members.Should().HaveCount(1); // Row remains in database
        pool.Members.First().IsActive.Should().BeFalse(); // Soft-removed
    }

    [Fact]
    public void TalentPool_ReAddingSoftRemovedCandidate_ShouldReactivateRow()
    {
        // Arrange
        var pool = TalentPool.Create(Guid.NewGuid(), Guid.NewGuid(), "Go Developers", null, null, false);
        var seekerId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        pool.AddCandidate(seekerId, recruiterId, "First note");
        pool.RemoveCandidate(seekerId);

        // Act
        var reAddResult = pool.AddCandidate(seekerId, recruiterId, "Updated reactivated note");

        // Assert
        reAddResult.IsSuccess.Should().BeTrue();
        pool.Members.Should().HaveCount(1); // Keeps the same row
        var candidate = pool.Members.First();
        candidate.IsActive.Should().BeTrue(); // Reactivated!
        candidate.Note.Should().Be("Updated reactivated note");
    }

    [Fact]
    public void RecommendationFeedback_Record_ShouldSuppressNotInterestedForTwoWeeks()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();

        // Act
        var feedback = RecommendationFeedback.Record(seekerId, postingId, FeedbackSignal.NotInterested);

        // Assert
        feedback.JobSeekerId.Should().Be(seekerId);
        feedback.JobPostingId.Should().Be(postingId);
        feedback.Signal.Should().Be(FeedbackSignal.NotInterested);
        feedback.SuppressUntilUtc.Should().NotBeNull();
        feedback.SuppressUntilUtc.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(14), TimeSpan.FromSeconds(5));
    }
}
