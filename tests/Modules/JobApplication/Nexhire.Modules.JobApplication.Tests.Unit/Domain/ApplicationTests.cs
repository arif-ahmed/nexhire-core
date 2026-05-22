using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Core.Domain.Events;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using Xunit;

namespace Nexhire.Modules.JobApplication.Tests.Unit.Domain;

public class ApplicationTests
{
    private readonly CandidateSnapshot _validSnapshot;

    public ApplicationTests()
    {
        var snapshotResult = CandidateSnapshot.Create(
            "Jane Smith",
            "jane@example.com",
            "0987654321",
            "Seattle",
            "M.Sc.",
            "3 years",
            new List<string> { "Go", "DDD" },
            isLevel2Complete: true,
            DateTime.UtcNow);
        _validSnapshot = snapshotResult.Value;
    }

    [Fact]
    public void Submit_Should_Succeed_And_RaiseEvent_WhenSnapshotIsValid()
    {
        // Arrange
        var postingId = Guid.NewGuid();
        var seekerId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();

        // Act
        var result = Application.Submit(
            postingId,
            seekerId,
            employerId,
            _validSnapshot,
            resumeId,
            null,
            85,
            idempotencyKey);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var application = result.Value;
        application.Status.Should().Be(ApplicationStatus.Submitted);
        application.JobPostingId.Should().Be(postingId);
        application.JobSeekerId.Should().Be(seekerId);
        application.EmployerId.Should().Be(employerId);
        application.ResumeDocumentId.Should().Be(resumeId);
        application.MatchScoreAtApply.Should().Be(85);
        application.IdempotencyKey.Should().Be(idempotencyKey);
        application.ReplacesApplicationId.Should().BeNull();

        // Stage history
        application.Stages.Should().HaveCount(1);
        var initialStage = application.Stages.First();
        initialStage.Stage.Should().Be(ApplicationStatus.Submitted);
        initialStage.EnteredByRole.Should().Be(StageActorRole.Seeker);
        initialStage.EnteredByUserId.Should().Be(seekerId);

        // Events
        application.DomainEvents.Should().HaveCount(1);
        var subEvent = application.DomainEvents.First().Should().BeOfType<ApplicationSubmittedIntegrationEvent>().Subject;
        subEvent.ApplicationId.Should().Be(application.Id.Value);
        subEvent.JobSeekerId.Should().Be(seekerId);
        subEvent.JobPostingId.Should().Be(postingId);
        subEvent.EmployerId.Should().Be(employerId);
        subEvent.Snapshot.FullName.Should().Be("Jane Smith");
        subEvent.Snapshot.Email.Should().Be("jane@example.com");
        subEvent.Snapshot.Skills.Should().Contain("Go");
        subEvent.MatchScoreAtApply.Should().Be(85);
    }

    [Fact]
    public void Withdraw_Should_Succeed_And_TransitionStatus_WhenApplicationIsSubmitted()
    {
        // Arrange
        var application = CreateSubmittedApplication();
        var withdrawalReason = WithdrawalReason.Create("ChangedMind", "No longer interested").Value;
        var seekerUserId = application.JobSeekerId;

        // Act
        var result = application.Withdraw(withdrawalReason, seekerUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        application.Status.Should().Be(ApplicationStatus.Withdrawn);
        application.WithdrawnOnUtc.Should().NotBeNull();

        // Stage history
        application.Stages.Should().HaveCount(2);
        var withdrawStage = application.Stages.Last();
        withdrawStage.Stage.Should().Be(ApplicationStatus.Withdrawn);
        withdrawStage.EnteredByRole.Should().Be(StageActorRole.Seeker);
        withdrawStage.EnteredByUserId.Should().Be(seekerUserId);
        withdrawStage.ReasonCode.Should().Be("ChangedMind");
        withdrawStage.Comment.Should().Be("No longer interested");

        // Events
        var withdrawnEvent = application.DomainEvents.Should().ContainSingle(e => e is ApplicationWithdrawnIntegrationEvent)
            .Subject.Should().BeOfType<ApplicationWithdrawnIntegrationEvent>().Subject;
        withdrawnEvent.ApplicationId.Should().Be(application.Id.Value);
        withdrawnEvent.JobSeekerId.Should().Be(application.JobSeekerId);
        withdrawnEvent.WithdrawalReasonCode.Should().Be("ChangedMind");
    }

    [Fact]
    public void Withdraw_Should_BeIdempotent_WhenAlreadyWithdrawn()
    {
        // Arrange
        var application = CreateSubmittedApplication();
        var withdrawalReason = WithdrawalReason.Create("ChangedMind", "First withdraw").Value;
        application.Withdraw(withdrawalReason, application.JobSeekerId);
        application.ClearDomainEvents();

        // Act
        var result = application.Withdraw(withdrawalReason, application.JobSeekerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        application.Status.Should().Be(ApplicationStatus.Withdrawn);
        application.DomainEvents.Should().BeEmpty(); // No new events raised
    }

    [Fact]
    public void Withdraw_Should_Fail_WhenApplicationIsHired()
    {
        // Arrange
        var application = CreateSubmittedApplication();
        var recruiterId = Guid.NewGuid();
        application.TransitionStage(ApplicationStatus.UnderReview, StageActorRole.Recruiter, recruiterId, null, null, ApplicationStatus.Submitted);
        application.TransitionStage(ApplicationStatus.Shortlisted, StageActorRole.Recruiter, recruiterId, null, null, ApplicationStatus.UnderReview);
        application.TransitionStage(ApplicationStatus.Interview, StageActorRole.Recruiter, recruiterId, null, null, ApplicationStatus.Shortlisted);
        application.TransitionStage(ApplicationStatus.Offered, StageActorRole.Recruiter, recruiterId, null, null, ApplicationStatus.Interview);
        application.TransitionStage(ApplicationStatus.Hired, StageActorRole.Recruiter, recruiterId, null, null, ApplicationStatus.Offered);
        var withdrawalReason = WithdrawalReason.Create("ChangedMind", "First withdraw").Value;

        // Act
        var result = application.Withdraw(withdrawalReason, application.JobSeekerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-INVALID-TRANSITION");
        application.Status.Should().Be(ApplicationStatus.Hired);
    }

    [Fact]
    public void MarkExpiredDueToPostingClosure_Should_Succeed_WhenApplicationIsSubmitted()
    {
        // Arrange
        var application = CreateSubmittedApplication();

        // Act
        var result = application.MarkExpiredDueToPostingClosure("posting-closed");

        // Assert
        result.IsSuccess.Should().BeTrue();
        application.Status.Should().Be(ApplicationStatus.Expired);

        // Stage history
        application.Stages.Should().HaveCount(2);
        var expiredStage = application.Stages.Last();
        expiredStage.Stage.Should().Be(ApplicationStatus.Expired);
        expiredStage.EnteredByRole.Should().Be(StageActorRole.System);
        expiredStage.ReasonCode.Should().Be("posting-closed");
    }

    [Fact]
    public void WithdrawDueToAccountDeactivation_Should_Succeed_And_WithdrawApplication()
    {
        // Arrange
        var application = CreateSubmittedApplication();

        // Act
        var result = application.WithdrawDueToAccountDeactivation();

        // Assert
        result.IsSuccess.Should().BeTrue();
        application.Status.Should().Be(ApplicationStatus.Withdrawn);

        // Stage history
        application.Stages.Should().HaveCount(2);
        var stage = application.Stages.Last();
        stage.Stage.Should().Be(ApplicationStatus.Withdrawn);
        stage.EnteredByRole.Should().Be(StageActorRole.System);
        stage.ReasonCode.Should().Be("AccountDeactivated");
    }

    [Fact]
    public void TransitionStage_Should_Fail_WhenExpectedStatusMismatches()
    {
        // Arrange
        var application = CreateSubmittedApplication();

        // Act
        var result = application.TransitionStage(
            ApplicationStatus.Shortlisted,
            StageActorRole.Recruiter,
            Guid.NewGuid(),
            reasonCode: null,
            comment: null,
            expectedCurrentStatus: ApplicationStatus.UnderReview); // Expected mismatch (is Submitted)

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-STALE");
        application.Status.Should().Be(ApplicationStatus.Submitted);
    }

    [Fact]
    public void TransitionStage_Should_Fail_WhenTransitionIsIllegal()
    {
        // Arrange
        var application = CreateSubmittedApplication();

        // Act
        var result = application.TransitionStage(
            ApplicationStatus.Hired, // Illegal directly from Submitted
            StageActorRole.Recruiter,
            Guid.NewGuid(),
            reasonCode: null,
            comment: null,
            expectedCurrentStatus: ApplicationStatus.Submitted);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-INVALID-TRANSITION");
        application.Status.Should().Be(ApplicationStatus.Submitted);
    }

    [Fact]
    public void TransitionStage_Should_Fail_WhenReasonIsRequiredButMissing()
    {
        // Arrange
        var application = CreateSubmittedApplication();

        // Act
        var result = application.TransitionStage(
            ApplicationStatus.Rejected, // Requires reason
            StageActorRole.Recruiter,
            Guid.NewGuid(),
            reasonCode: null, // Missing!
            comment: null,
            expectedCurrentStatus: ApplicationStatus.Submitted);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-REASON-REQUIRED");
        application.Status.Should().Be(ApplicationStatus.Submitted);
    }

    [Fact]
    public void TransitionStage_Should_Succeed_And_RaiseEvent_WhenTransitionIsValidAndReasonIsSupplied()
    {
        // Arrange
        var application = CreateSubmittedApplication();
        var recruiterId = Guid.NewGuid();

        // Act
        var result = application.TransitionStage(
            ApplicationStatus.Rejected,
            StageActorRole.Recruiter,
            recruiterId,
            reasonCode: "QualificationsNotMatching",
            comment: "Experience missing.",
            expectedCurrentStatus: ApplicationStatus.Submitted);

        // Assert
        result.IsSuccess.Should().BeTrue();
        application.Status.Should().Be(ApplicationStatus.Rejected);
        application.RejectedOnUtc.Should().NotBeNull();

        // Events
        application.DomainEvents.Should().Contain(e => e is ApplicationRejectedDomainEvent);
        application.DomainEvents.Should().Contain(e => e is ApplicationStatusChangedIntegrationEvent);
    }

    private Application CreateSubmittedApplication()
    {
        return Application.Submit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _validSnapshot,
            Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid()).Value;
    }
}
