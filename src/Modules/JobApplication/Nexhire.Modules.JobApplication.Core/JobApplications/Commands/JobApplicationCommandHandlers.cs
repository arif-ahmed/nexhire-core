using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Core.Domain.Ports;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;
using Nexhire.Modules.JobApplication.Core.Domain.Services;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using ApplicationId = Nexhire.Modules.JobApplication.Core.Domain.ApplicationId;

namespace Nexhire.Modules.JobApplication.Core.JobApplications.Commands;

public sealed class AddBookmarkCommandHandler : ICommandHandler<AddBookmarkCommand, Guid>
{
    private readonly IBookmarkRepository _bookmarkRepository;
    private readonly IJobApplicationUnitOfWork _unitOfWork;

    public AddBookmarkCommandHandler(IBookmarkRepository bookmarkRepository, IJobApplicationUnitOfWork unitOfWork)
    {
        _bookmarkRepository = bookmarkRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddBookmarkCommand request, CancellationToken cancellationToken)
    {
        var existing = await _bookmarkRepository.GetAsync(request.JobSeekerId, request.JobPostingId, cancellationToken);
        if (existing != null)
        {
            return Result.Failure<Guid>(new Error("E-BOOKMARK-DUPLICATE", "This job is already bookmarked."));
        }

        var bookmark = Bookmark.Create(request.JobSeekerId, request.JobPostingId);
        await _bookmarkRepository.AddAsync(bookmark, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(bookmark.Id.Value);
    }
}

public sealed class RemoveBookmarkCommandHandler : ICommandHandler<RemoveBookmarkCommand>
{
    private readonly IBookmarkRepository _bookmarkRepository;
    private readonly IJobApplicationUnitOfWork _unitOfWork;

    public RemoveBookmarkCommandHandler(IBookmarkRepository bookmarkRepository, IJobApplicationUnitOfWork unitOfWork)
    {
        _bookmarkRepository = bookmarkRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveBookmarkCommand request, CancellationToken cancellationToken)
    {
        var bookmark = await _bookmarkRepository.GetAsync(request.JobSeekerId, request.JobPostingId, cancellationToken);
        if (bookmark == null)
        {
            return Result.Failure(new Error("E-BOOKMARK-NOT-FOUND", "Bookmark not found."));
        }

        bookmark.Unbookmark();
        _bookmarkRepository.Remove(bookmark);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class SubmitApplicationCommandHandler : ICommandHandler<SubmitApplicationCommand, SubmitApplicationResponse>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobPostingApi _jobPostingApi;
    private readonly IJobSeekerProfileApi _jobSeekerProfileApi;
    private readonly IMatchRankingPublicApi _matchRankingPublicApi;
    private readonly IIdempotencyKeyStore _idempotencyKeyStore;
    private readonly IJobApplicationUnitOfWork _unitOfWork;

    public SubmitApplicationCommandHandler(
        IApplicationRepository applicationRepository,
        IJobPostingApi jobPostingApi,
        IJobSeekerProfileApi jobSeekerProfileApi,
        IMatchRankingPublicApi matchRankingPublicApi,
        IIdempotencyKeyStore idempotencyKeyStore,
        IJobApplicationUnitOfWork unitOfWork)
    {
        _applicationRepository = applicationRepository;
        _jobPostingApi = jobPostingApi;
        _jobSeekerProfileApi = jobSeekerProfileApi;
        _matchRankingPublicApi = matchRankingPublicApi;
        _idempotencyKeyStore = idempotencyKeyStore;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SubmitApplicationResponse>> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        // 1. Idempotency check:
        var existingApplicationId = await _idempotencyKeyStore.TryGetAsync(request.IdempotencyKey, cancellationToken);
        if (existingApplicationId.HasValue)
        {
            var app = await _applicationRepository.GetByIdAsync(new ApplicationId(existingApplicationId.Value), cancellationToken);
            if (app != null)
            {
                return Result.Success(new SubmitApplicationResponse(
                    app.Id.Value,
                    app.Status.ToString(),
                    app.MatchScoreAtApply));
            }
        }

        // 2. Fetch posting details from JobPostingApi:
        var postingResult = await _jobPostingApi.GetApplicabilityAsync(request.JobPostingId, cancellationToken);
        if (postingResult.IsFailure)
        {
            return Result.Failure<SubmitApplicationResponse>(postingResult.Error);
        }
        var posting = postingResult.Value;

        // 3. Check level 2 gate via JobSeekerProfileApi:
        var isL2 = await _jobSeekerProfileApi.IsLevel2CompleteAsync(request.JobSeekerId, cancellationToken);
        
        // 4. Check if resume is usable:
        var resumeResult = await _jobSeekerProfileApi.IsResumeUsableAsync(request.JobSeekerId, request.ResumeDocumentId, cancellationToken);
        if (resumeResult.IsFailure)
        {
            return Result.Failure<SubmitApplicationResponse>(resumeResult.Error);
        }
        if (!resumeResult.Value)
        {
            return Result.Failure<SubmitApplicationResponse>(new Error("E-APP-RESUME-MISSING", "The selected resume is not available or invalid."));
        }

        // 5. Load any existing non-terminal application for (seeker, posting):
        var existingNonTerminalApp = await _applicationRepository.GetNonTerminalForAsync(request.JobSeekerId, request.JobPostingId, cancellationToken);

        // 6. Enforce eligibility rules via ApplicationEligibilityService:
        var eligibilityResult = ApplicationEligibilityService.CheckCanApply(
            request.JobSeekerId,
            request.JobPostingId,
            posting,
            isL2,
            existingNonTerminalApp);

        if (eligibilityResult.IsFailure)
        {
            return Result.Failure<SubmitApplicationResponse>(eligibilityResult.Error);
        }

        // 7. Get the job seeker profile snapshot:
        var profileResult = await _jobSeekerProfileApi.GetSnapshotAsync(request.JobSeekerId, cancellationToken);
        if (profileResult.IsFailure)
        {
            return Result.Failure<SubmitApplicationResponse>(profileResult.Error);
        }

        // 8. Build CandidateSnapshot using CandidateSnapshotBuilder:
        var snapshotResult = CandidateSnapshotBuilder.Build(profileResult.Value, request.Overrides);
        if (snapshotResult.IsFailure)
        {
            return Result.Failure<SubmitApplicationResponse>(snapshotResult.Error);
        }

        // 9. Fetch cover letter if provided:
        CoverLetter? coverLetter = null;
        if (!string.IsNullOrWhiteSpace(request.CoverLetter))
        {
            var coverLetterResult = CoverLetter.Create(request.CoverLetter);
            if (coverLetterResult.IsFailure)
            {
                return Result.Failure<SubmitApplicationResponse>(coverLetterResult.Error);
            }
            coverLetter = coverLetterResult.Value;
        }

        // 10. Fetch match score from Recommendation Engine (best-effort):
        int? matchScore = null;
        try
        {
            var scoreResult = await _matchRankingPublicApi.GetMatchScoreAsync(request.JobSeekerId, request.JobPostingId, cancellationToken);
            if (scoreResult.IsSuccess)
            {
                matchScore = scoreResult.Value;
            }
        }
        catch
        {
            // Fail-safe, non-blocking as per spec
        }

        // 11. Check if a prior terminal application exists for ReplacesApplicationId:
        ApplicationId? replacesApplicationId = null;
        var terminalApps = await _applicationRepository.GetTerminalForAsync(request.JobSeekerId, request.JobPostingId, cancellationToken);
        if (terminalApps.Count > 0)
        {
            var sorted = terminalApps.OrderByDescending(a => a.AppliedOnUtc).First();
            replacesApplicationId = sorted.Id;
        }

        // 12. Submit the application aggregate:
        var submitResult = Application.Submit(
            request.JobPostingId,
            request.JobSeekerId,
            posting.EmployerId,
            snapshotResult.Value,
            request.ResumeDocumentId,
            coverLetter,
            matchScore,
            request.IdempotencyKey,
            replacesApplicationId);

        if (submitResult.IsFailure)
        {
            return Result.Failure<SubmitApplicationResponse>(submitResult.Error);
        }

        var application = submitResult.Value;

        // 13. Persist application and idempotency key:
        await _applicationRepository.AddAsync(application, cancellationToken);
        await _idempotencyKeyStore.SaveAsync(request.IdempotencyKey, application.Id, cancellationToken);

        // 14. Commit transaction:
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SubmitApplicationResponse(
            application.Id.Value,
            application.Status.ToString(),
            application.MatchScoreAtApply));
    }
}

public sealed class WithdrawApplicationCommandHandler : ICommandHandler<WithdrawApplicationCommand, WithdrawApplicationResponse>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobApplicationUnitOfWork _unitOfWork;

    public WithdrawApplicationCommandHandler(IApplicationRepository applicationRepository, IJobApplicationUnitOfWork unitOfWork)
    {
        _applicationRepository = applicationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WithdrawApplicationResponse>> Handle(WithdrawApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(new ApplicationId(request.ApplicationId), cancellationToken);
        if (application == null)
        {
            return Result.Failure<WithdrawApplicationResponse>(new Error("E-APP-NOT-FOUND", "Application was not found."));
        }

        if (application.JobSeekerId != request.JobSeekerId)
        {
            return Result.Failure<WithdrawApplicationResponse>(new Error("E-APP-FORBIDDEN", "Forbidden: You do not own this application."));
        }

        var reasonResult = WithdrawalReason.Create(request.ReasonCode, request.Comment);
        if (reasonResult.IsFailure)
        {
            return Result.Failure<WithdrawApplicationResponse>(reasonResult.Error);
        }

        var withdrawResult = application.Withdraw(reasonResult.Value, request.JobSeekerId);
        if (withdrawResult.IsFailure)
        {
            return Result.Failure<WithdrawApplicationResponse>(withdrawResult.Error);
        }

        _applicationRepository.Update(application);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new WithdrawApplicationResponse(
            application.Status.ToString(),
            application.WithdrawnOnUtc!.Value));
    }
}
