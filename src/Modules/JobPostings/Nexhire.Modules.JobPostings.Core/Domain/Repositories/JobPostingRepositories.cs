using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobPostings.Core.Domain.Repositories;

public interface IJobPostingRepository
{
    Task<JobPosting?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JobPosting>> GetByEmployerIdAsync(Guid employerId, PostingStatus? status, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JobPosting>> SearchAsync(JobPostingSearchFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JobPosting>> GetOpenByEmployerIdAsync(Guid employerId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JobPosting>> GetBySkillCodesAsync(IReadOnlyCollection<string> skillCodes, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JobPosting>> GetExpirableAsync(DateTime nowUtc, CancellationToken cancellationToken);
    Task<JobPosting?> GetByExternalRefAsync(string externalRef, CancellationToken cancellationToken);
    Task AddAsync(JobPosting posting, CancellationToken cancellationToken);
}

public sealed record JobPostingSearchFilter(
    Guid? EmployerId,
    PostingStatus? Status,
    DateTime? PostedFromUtc,
    DateTime? PostedToUtc,
    string? Location,
    string? Query);

public interface IPostingAuditTrailRepository
{
    Task<PostingAuditTrail?> GetByPostingIdAsync(Guid jobPostingId, CancellationToken cancellationToken);
    Task AddAsync(PostingAuditTrail trail, CancellationToken cancellationToken);
}

public interface IEmployerStandingStore
{
    Task<EmployerStanding?> GetAsync(Guid employerId, CancellationToken cancellationToken);
    Task UpsertAsync(EmployerStanding standing, CancellationToken cancellationToken);
}

public interface IPostingMetricsStore
{
    Task<PostingMetrics?> GetAsync(Guid jobPostingId, CancellationToken cancellationToken);
    Task UpsertAsync(PostingMetrics metrics, CancellationToken cancellationToken);
}

public interface IJobPostingsUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class PostingMetrics
{
    public Guid JobPostingId { get; private set; }
    public int ApplicationsCount { get; private set; }
    public int MatchesCount { get; private set; }
    public int ViewsCount { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private PostingMetrics() { }

    public PostingMetrics(Guid jobPostingId, int applicationsCount, int matchesCount, int viewsCount, DateTime updatedOnUtc)
    {
        JobPostingId = jobPostingId;
        ApplicationsCount = applicationsCount;
        MatchesCount = matchesCount;
        ViewsCount = viewsCount;
        UpdatedOnUtc = updatedOnUtc;
    }

    public PostingMetrics WithApplications(int count, DateTime updatedOnUtc) => new(JobPostingId, count, MatchesCount, ViewsCount, updatedOnUtc);
    public PostingMetrics WithMatches(int count, DateTime updatedOnUtc) => new(JobPostingId, ApplicationsCount, count, ViewsCount, updatedOnUtc);
    public PostingMetrics WithViews(int count, DateTime updatedOnUtc) => new(JobPostingId, ApplicationsCount, MatchesCount, count, updatedOnUtc);
}
