using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Core.Domain.Ports;

public interface IJobPostingApi
{
    Task<Result<PostingApplicabilitySnapshot>> GetApplicabilityAsync(Guid jobPostingId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<PostingSummaryDto>>> GetSummariesAsync(IEnumerable<Guid> jobPostingIds, CancellationToken cancellationToken);
}

public record PostingSummaryDto(
    Guid JobPostingId,
    string Title,
    string CompanyName,
    string Location,
    string? SalaryDisplay,
    string Status
);

public interface IJobSeekerProfileApi
{
    Task<bool> IsLevel2CompleteAsync(Guid jobSeekerProfileId, CancellationToken cancellationToken);
    Task<Result<JobSeekerProfileSnapshotDto>> GetSnapshotAsync(Guid jobSeekerProfileId, CancellationToken cancellationToken);
    Task<Result<bool>> IsResumeUsableAsync(Guid jobSeekerProfileId, Guid resumeDocumentId, CancellationToken cancellationToken);
}

public interface IMatchRankingPublicApi
{
    Task<Result<int?>> GetMatchScoreAsync(Guid jobSeekerId, Guid jobPostingId, CancellationToken cancellationToken);
}

public interface IEmployerAccessApi
{
    Task<bool> CanRecruiterAccessPostingAsync(Guid recruiterId, Guid jobPostingId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<Guid>>> GetPostingsForRecruiterAsync(Guid recruiterId, CancellationToken cancellationToken);
}
