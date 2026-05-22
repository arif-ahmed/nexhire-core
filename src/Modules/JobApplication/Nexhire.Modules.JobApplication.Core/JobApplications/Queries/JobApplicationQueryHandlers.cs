using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.JobApplication.Core.Domain.Ports;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using ApplicationId = Nexhire.Modules.JobApplication.Core.Domain.ApplicationId;

namespace Nexhire.Modules.JobApplication.Core.JobApplications.Queries;

public sealed class GetMyBookmarksQueryHandler : IQueryHandler<GetMyBookmarksQuery, IReadOnlyCollection<BookmarkedJobDto>>
{
    private readonly IBookmarkRepository _bookmarkRepository;
    private readonly IJobPostingApi _jobPostingApi;

    public GetMyBookmarksQueryHandler(IBookmarkRepository bookmarkRepository, IJobPostingApi jobPostingApi)
    {
        _bookmarkRepository = bookmarkRepository;
        _jobPostingApi = jobPostingApi;
    }

    public async Task<Result<IReadOnlyCollection<BookmarkedJobDto>>> Handle(GetMyBookmarksQuery request, CancellationToken cancellationToken)
    {
        var bookmarks = await _bookmarkRepository.ListBySeekerAsync(request.JobSeekerId, cancellationToken);
        if (bookmarks.Count == 0)
        {
            return Result.Success<IReadOnlyCollection<BookmarkedJobDto>>(Array.Empty<BookmarkedJobDto>());
        }

        var postingIds = bookmarks.Select(b => b.JobPostingId).Distinct().ToList();
        var postingResult = await _jobPostingApi.GetSummariesAsync(postingIds, cancellationToken);
        if (postingResult.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<BookmarkedJobDto>>(postingResult.Error);
        }

        var postingMap = postingResult.Value.ToDictionary(p => p.JobPostingId);

        var result = bookmarks.Select(b =>
        {
            postingMap.TryGetValue(b.JobPostingId, out var posting);
            return new BookmarkedJobDto(
                b.Id.Value,
                b.JobPostingId,
                b.BookmarkedOnUtc,
                posting?.Title ?? "Unknown Title",
                posting?.CompanyName ?? "Unknown Company",
                posting?.Location ?? "Unknown Location",
                posting?.SalaryDisplay,
                posting?.Status ?? "Unknown"
            );
        }).ToList();

        return Result.Success<IReadOnlyCollection<BookmarkedJobDto>>(result);
    }
}

public sealed class GetMyApplicationsQueryHandler : IQueryHandler<GetMyApplicationsQuery, PagedResult<ApplicationListItemDto>>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobPostingApi _jobPostingApi;

    public GetMyApplicationsQueryHandler(IApplicationRepository applicationRepository, IJobPostingApi jobPostingApi)
    {
        _applicationRepository = applicationRepository;
        _jobPostingApi = jobPostingApi;
    }

    public async Task<Result<PagedResult<ApplicationListItemDto>>> Handle(GetMyApplicationsQuery request, CancellationToken cancellationToken)
    {
        var applications = await _applicationRepository.ListBySeekerAsync(request.JobSeekerId, cancellationToken);

        // 1. In-memory filter & sort
        var query = applications.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(a => string.Equals(a.Status.ToString(), request.Status, StringComparison.OrdinalIgnoreCase));
        }

        var sorted = query.OrderByDescending(a => a.AppliedOnUtc).ToList();
        var totalCount = sorted.Count;

        // 2. Pagination
        var pagedApps = sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        if (pagedApps.Count == 0)
        {
            return Result.Success(new PagedResult<ApplicationListItemDto>(
                Array.Empty<ApplicationListItemDto>(),
                request.Page,
                request.PageSize,
                totalCount));
        }

        // 3. Get posting summaries
        var postingIds = pagedApps.Select(a => a.JobPostingId).Distinct().ToList();
        var postingResult = await _jobPostingApi.GetSummariesAsync(postingIds, cancellationToken);
        if (postingResult.IsFailure)
        {
            return Result.Failure<PagedResult<ApplicationListItemDto>>(postingResult.Error);
        }

        var postingMap = postingResult.Value.ToDictionary(p => p.JobPostingId);

        var items = pagedApps.Select(a =>
        {
            postingMap.TryGetValue(a.JobPostingId, out var posting);
            return new ApplicationListItemDto(
                a.Id.Value,
                a.JobPostingId,
                posting?.Title ?? "Unknown Title",
                posting?.CompanyName ?? "Unknown Company",
                a.Status.ToString(),
                a.AppliedOnUtc,
                a.LastStatusChangeOnUtc
            );
        }).ToList();

        return Result.Success(new PagedResult<ApplicationListItemDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount));
    }
}

public sealed class GetMyApplicationDetailQueryHandler : IQueryHandler<GetMyApplicationDetailQuery, ApplicationDetailDto>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobPostingApi _jobPostingApi;

    public GetMyApplicationDetailQueryHandler(IApplicationRepository applicationRepository, IJobPostingApi jobPostingApi)
    {
        _applicationRepository = applicationRepository;
        _jobPostingApi = jobPostingApi;
    }

    public async Task<Result<ApplicationDetailDto>> Handle(GetMyApplicationDetailQuery request, CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(new ApplicationId(request.ApplicationId), cancellationToken);
        if (application == null)
        {
            return Result.Failure<ApplicationDetailDto>(new Error("E-APP-NOT-FOUND", "Application not found."));
        }

        if (application.JobSeekerId != request.JobSeekerId)
        {
            return Result.Failure<ApplicationDetailDto>(new Error("E-APP-FORBIDDEN", "Forbidden: You do not own this application."));
        }

        // Fetch posting detail
        PostingSummaryDto? posting = null;
        var postingResult = await _jobPostingApi.GetSummariesAsync(new[] { application.JobPostingId }, cancellationToken);
        if (postingResult.IsSuccess)
        {
            posting = postingResult.Value.FirstOrDefault();
        }

        var candidateSnapshotDto = new CandidateSnapshotDto(
            application.CandidateSnapshot.FullName,
            application.CandidateSnapshot.Email,
            application.CandidateSnapshot.Mobile,
            application.CandidateSnapshot.CurrentLocation,
            application.CandidateSnapshot.EducationSummary,
            application.CandidateSnapshot.ExperienceSummary,
            application.CandidateSnapshot.Skills,
            application.CandidateSnapshot.CapturedOnUtc
        );

        var stagesDto = application.Stages.Select(s => new ApplicationStageDto(
            s.Stage.ToString(),
            s.EnteredOnUtc,
            s.EnteredByRole.ToString(),
            s.ReasonCode,
            s.Comment
        )).ToList();

        var detail = new ApplicationDetailDto(
            application.Id.Value,
            application.JobPostingId,
            application.JobSeekerId,
            application.EmployerId,
            application.Status.ToString(),
            candidateSnapshotDto,
            application.ResumeDocumentId,
            application.CoverLetter?.Text,
            application.MatchScoreAtApply,
            application.ReplacesApplicationId?.Value,
            application.AppliedOnUtc,
            application.LastStatusChangeOnUtc,
            stagesDto,
            posting
        );

        return Result.Success(detail);
    }
}
