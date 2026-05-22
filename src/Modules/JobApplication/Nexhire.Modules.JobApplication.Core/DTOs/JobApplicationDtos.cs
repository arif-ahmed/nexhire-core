using System;
using System.Collections.Generic;
using Nexhire.Modules.JobApplication.Core.Domain.Ports;

namespace Nexhire.Modules.JobApplication.Core.DTOs;

public record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    long TotalCount)
{
    public bool NoResults => TotalCount == 0;
}

public record BookmarkedJobDto(
    Guid BookmarkId,
    Guid JobPostingId,
    DateTime BookmarkedOnUtc,
    string Title,
    string CompanyName,
    string Location,
    string? SalaryDisplay,
    string Status
);

public record ApplicationListItemDto(
    Guid ApplicationId,
    Guid JobPostingId,
    string Title,
    string CompanyName,
    string Status,
    DateTime AppliedOnUtc,
    DateTime LastStatusChangeOnUtc
);

public record CandidateSnapshotDto(
    string FullName,
    string Email,
    string Mobile,
    string CurrentLocation,
    string EducationSummary,
    string ExperienceSummary,
    IReadOnlyList<string> Skills,
    DateTime CapturedOnUtc
);

public record ApplicationStageDto(
    string Stage,
    DateTime EnteredOnUtc,
    string EnteredByRole,
    string? ReasonCode,
    string? Comment
);

public record ApplicationDetailDto(
    Guid ApplicationId,
    Guid JobPostingId,
    Guid JobSeekerId,
    Guid EmployerId,
    string Status,
    CandidateSnapshotDto CandidateSnapshot,
    Guid ResumeDocumentId,
    string? CoverLetter,
    int? MatchScoreAtApply,
    Guid? ReplacesApplicationId,
    DateTime AppliedOnUtc,
    DateTime LastStatusChangeOnUtc,
    IReadOnlyList<ApplicationStageDto> Stages,
    PostingSummaryDto? Posting
);
