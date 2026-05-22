using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Modules.JobApplication.Core.Domain.Events;
using Nexhire.Modules.JobApplication.Core.Domain.Services;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Core.Domain;

public class Application : AggregateRoot<ApplicationId>
{
    private readonly List<ApplicationStage> _stages = new();

    public Guid JobPostingId { get; private set; }
    public Guid JobSeekerId { get; private set; }
    public Guid EmployerId { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public CandidateSnapshot CandidateSnapshot { get; private set; } = null!;
    public Guid ResumeDocumentId { get; private set; }
    public CoverLetter? CoverLetter { get; private set; }
    public int? MatchScoreAtApply { get; private set; }
    public ApplicationId? ReplacesApplicationId { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public DateTime AppliedOnUtc { get; private set; }
    public DateTime LastStatusChangeOnUtc { get; private set; }
    public DateTime? WithdrawnOnUtc { get; private set; }
    public DateTime? HiredOnUtc { get; private set; }
    public DateTime? RejectedOnUtc { get; private set; }
    public Guid Version { get; private set; }

    public IReadOnlyCollection<ApplicationStage> Stages => _stages.AsReadOnly();

    private Application(
        ApplicationId id,
        Guid jobPostingId,
        Guid jobSeekerId,
        Guid employerId,
        CandidateSnapshot candidateSnapshot,
        Guid resumeDocumentId,
        CoverLetter? coverLetter,
        int? matchScoreAtApply,
        ApplicationId? replacesApplicationId,
        Guid idempotencyKey,
        DateTime appliedOnUtc) : base(id)
    {
        JobPostingId = jobPostingId;
        JobSeekerId = jobSeekerId;
        EmployerId = employerId;
        Status = ApplicationStatus.Submitted;
        CandidateSnapshot = candidateSnapshot;
        ResumeDocumentId = resumeDocumentId;
        CoverLetter = coverLetter;
        MatchScoreAtApply = matchScoreAtApply;
        ReplacesApplicationId = replacesApplicationId;
        IdempotencyKey = idempotencyKey;
        AppliedOnUtc = appliedOnUtc;
        LastStatusChangeOnUtc = appliedOnUtc;
        Version = Guid.NewGuid();
    }

    private Application()
    {
        // Required by EF Core
    }

    public static Result<Application> Submit(
        Guid jobPostingId,
        Guid jobSeekerId,
        Guid employerId,
        CandidateSnapshot candidateSnapshot,
        Guid resumeDocumentId,
        CoverLetter? coverLetter,
        int? matchScoreAtApply,
        Guid idempotencyKey,
        ApplicationId? replacesApplicationId = null)
    {
        if (!candidateSnapshot.IsLevel2Complete)
        {
            return Result.Failure<Application>(new Error("E-APP-PROFILE-INCOMPLETE", "Profile Level 2 must be complete."));
        }

        var applicationId = ApplicationId.New();
        var appliedOnUtc = DateTime.UtcNow;

        var application = new Application(
            applicationId,
            jobPostingId,
            jobSeekerId,
            employerId,
            candidateSnapshot,
            resumeDocumentId,
            coverLetter,
            matchScoreAtApply,
            replacesApplicationId,
            idempotencyKey,
            appliedOnUtc);

        var initialStage = ApplicationStage.Create(
            ApplicationStatus.Submitted,
            appliedOnUtc,
            StageActorRole.Seeker,
            jobSeekerId,
            comment: "Application formally submitted.");

        application._stages.Add(initialStage);

        var fingerprint = new CompactSnapshotFingerprint(
            candidateSnapshot.FullName,
            candidateSnapshot.Email,
            candidateSnapshot.IsLevel2Complete,
            resumeDocumentId,
            candidateSnapshot.Skills.ToList());

        application.RaiseDomainEvent(new ApplicationSubmittedIntegrationEvent(
            Guid.NewGuid(),
            applicationId.Value,
            jobSeekerId,
            jobPostingId,
            employerId,
            fingerprint,
            matchScoreAtApply,
            appliedOnUtc,
            appliedOnUtc));

        return application;
    }

    public Result Withdraw(WithdrawalReason withdrawalReason, Guid seekerUserId)
    {
        if (Status == ApplicationStatus.Withdrawn)
        {
            return Result.Success(); // Idempotent success
        }

        if (ApplicationStatusTransitionPolicy.IsTerminal(Status))
        {
            return Result.Failure(new Error("E-APP-INVALID-TRANSITION", $"Cannot withdraw from terminal status '{Status}'."));
        }

        var now = DateTime.UtcNow;
        var oldStatus = Status;
        Status = ApplicationStatus.Withdrawn;
        WithdrawnOnUtc = now;
        LastStatusChangeOnUtc = now;
        Version = Guid.NewGuid();

        var stage = ApplicationStage.Create(
            ApplicationStatus.Withdrawn,
            now,
            StageActorRole.Seeker,
            seekerUserId,
            withdrawalReason.Code,
            withdrawalReason.Comment);

        _stages.Add(stage);

        RaiseDomainEvent(new ApplicationWithdrawnIntegrationEvent(
            Guid.NewGuid(),
            Id.Value,
            JobSeekerId,
            withdrawalReason.Code,
            now));

        RaiseDomainEvent(new ApplicationStatusChangedIntegrationEvent(
            Guid.NewGuid(),
            Id.Value,
            oldStatus.ToString(),
            Status.ToString(),
            StageActorRole.Seeker.ToString(),
            now));

        return Result.Success();
    }

    public Result MarkExpiredDueToPostingClosure(string reason)
    {
        if (ApplicationStatusTransitionPolicy.IsTerminal(Status))
        {
            return Result.Success(); // No-op for terminal states
        }

        var now = DateTime.UtcNow;
        var oldStatus = Status;
        Status = ApplicationStatus.Expired;
        LastStatusChangeOnUtc = now;
        Version = Guid.NewGuid();

        var stage = ApplicationStage.Create(
            ApplicationStatus.Expired,
            now,
            StageActorRole.System,
            reasonCode: reason,
            comment: $"Application expired because: {reason}.");

        _stages.Add(stage);

        RaiseDomainEvent(new ApplicationStatusChangedIntegrationEvent(
            Guid.NewGuid(),
            Id.Value,
            oldStatus.ToString(),
            Status.ToString(),
            StageActorRole.System.ToString(),
            now));

        return Result.Success();
    }

    public Result WithdrawDueToAccountDeactivation()
    {
        if (ApplicationStatusTransitionPolicy.IsTerminal(Status))
        {
            return Result.Success(); // No-op
        }

        var now = DateTime.UtcNow;
        var oldStatus = Status;
        Status = ApplicationStatus.Withdrawn;
        WithdrawnOnUtc = now;
        LastStatusChangeOnUtc = now;
        Version = Guid.NewGuid();

        var stage = ApplicationStage.Create(
            ApplicationStatus.Withdrawn,
            now,
            StageActorRole.System,
            reasonCode: "AccountDeactivated",
            comment: "Application withdrawn due to job seeker account deactivation.");

        _stages.Add(stage);

        RaiseDomainEvent(new ApplicationWithdrawnIntegrationEvent(
            Guid.NewGuid(),
            Id.Value,
            JobSeekerId,
            "AccountDeactivated",
            now));

        RaiseDomainEvent(new ApplicationStatusChangedIntegrationEvent(
            Guid.NewGuid(),
            Id.Value,
            oldStatus.ToString(),
            Status.ToString(),
            StageActorRole.System.ToString(),
            now));

        return Result.Success();
    }

    public Result TransitionStage(
        ApplicationStatus toStatus,
        StageActorRole byRole,
        Guid byUserId,
        string? reasonCode,
        string? comment,
        ApplicationStatus expectedCurrentStatus)
    {
        if (Status != expectedCurrentStatus)
        {
            return Result.Failure(new Error("E-APP-STALE", "Optimistic concurrency check failed: Application status is stale."));
        }

        if (!ApplicationStatusTransitionPolicy.IsTransitionAllowed(Status, toStatus))
        {
            return Result.Failure(new Error("E-APP-INVALID-TRANSITION", $"Transition from '{Status}' to '{toStatus}' is illegal."));
        }

        if (ApplicationStatusTransitionPolicy.RequiresReason(toStatus) && string.IsNullOrWhiteSpace(reasonCode))
        {
            return Result.Failure(new Error("E-APP-REASON-REQUIRED", $"A reason is required to transition to status '{toStatus}'."));
        }

        var now = DateTime.UtcNow;
        var oldStatus = Status;
        Status = toStatus;
        LastStatusChangeOnUtc = now;
        Version = Guid.NewGuid();

        if (toStatus == ApplicationStatus.Rejected)
        {
            RejectedOnUtc = now;
            RaiseDomainEvent(new ApplicationRejectedDomainEvent(Guid.NewGuid(), Id.Value, now));
        }
        else if (toStatus == ApplicationStatus.Hired)
        {
            HiredOnUtc = now;
        }

        var stage = ApplicationStage.Create(
            toStatus,
            now,
            byRole,
            byUserId,
            reasonCode,
            comment);

        _stages.Add(stage);

        RaiseDomainEvent(new ApplicationStatusChangedIntegrationEvent(
            Guid.NewGuid(),
            Id.Value,
            oldStatus.ToString(),
            toStatus.ToString(),
            byRole.ToString(),
            now));

        return Result.Success();
    }

    public void RecordView(Guid byEmployerId)
    {
        RaiseDomainEvent(new ApplicationViewedIntegrationEvent(
            Guid.NewGuid(),
            Id.Value,
            byEmployerId,
            DateTime.UtcNow));
    }
}
