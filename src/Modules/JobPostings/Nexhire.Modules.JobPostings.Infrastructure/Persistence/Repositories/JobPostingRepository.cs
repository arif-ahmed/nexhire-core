using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.Repositories;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobPostings.Infrastructure.Persistence.Repositories;

public sealed class JobPostingRepository : IJobPostingRepository
{
    private readonly JobPostingsDbContext _dbContext;
    public JobPostingRepository(JobPostingsDbContext dbContext) => _dbContext = dbContext;

    public Task<JobPosting?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.JobPostings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<JobPosting>> GetByEmployerIdAsync(Guid employerId, PostingStatus? status, CancellationToken cancellationToken) =>
        await _dbContext.JobPostings
            .Where(x => x.EmployerId == employerId && (status == null || x.Status == status))
            .OrderByDescending(x => x.UpdatedOnUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<JobPosting>> SearchAsync(JobPostingSearchFilter filter, CancellationToken cancellationToken)
    {
        var query = _dbContext.JobPostings.AsQueryable();

        if (filter.EmployerId is not null)
        {
            query = query.Where(x => x.EmployerId == filter.EmployerId);
        }
        if (filter.Status is not null)
        {
            query = query.Where(x => x.Status == filter.Status);
        }
        if (filter.PostedFromUtc is not null)
        {
            query = query.Where(x => x.CreatedOnUtc >= filter.PostedFromUtc);
        }
        if (filter.PostedToUtc is not null)
        {
            query = query.Where(x => x.CreatedOnUtc <= filter.PostedToUtc);
        }
        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = filter.Query.Trim().ToLower();
            query = query.Where(x => x.Title.Value.ToLower().Contains(term) || x.Summary.Value.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            var term = filter.Location.Trim().ToLower();
            query = query.Where(x => x.Location != null &&
                (x.Location.City.ToLower().Contains(term) ||
                 x.Location.District.ToLower().Contains(term) ||
                 x.Location.Country.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(x => x.UpdatedOnUtc).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<JobPosting>> GetOpenByEmployerIdAsync(Guid employerId, CancellationToken cancellationToken) =>
        await _dbContext.JobPostings
            .Where(x => x.EmployerId == employerId && (x.Status == PostingStatus.Active || x.Status == PostingStatus.Paused))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<JobPosting>> GetBySkillCodesAsync(IReadOnlyCollection<string> skillCodes, CancellationToken cancellationToken)
    {
        if (skillCodes.Count == 0)
        {
            return Array.Empty<JobPosting>();
        }

        var normalized = skillCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var postings = await _dbContext.JobPostings.ToListAsync(cancellationToken);
        return postings
            .Where(x => x.RequiredSkills.Any(skill => normalized.Contains(skill.CanonicalRef.TaxonomyCode)))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<JobPosting>> GetExpirableAsync(DateTime nowUtc, CancellationToken cancellationToken) =>
        await _dbContext.JobPostings
            .Where(x => (x.Status == PostingStatus.Active || x.Status == PostingStatus.Paused) && x.Deadline.DateUtc <= nowUtc)
            .ToListAsync(cancellationToken);

    public Task<JobPosting?> GetByExternalRefAsync(string externalRef, CancellationToken cancellationToken) =>
        _dbContext.JobPostings.FirstOrDefaultAsync(x => x.ExternalRef == externalRef, cancellationToken);

    public Task AddAsync(JobPosting posting, CancellationToken cancellationToken) =>
        _dbContext.JobPostings.AddAsync(posting, cancellationToken).AsTask();
}

public sealed class PostingAuditTrailRepository : IPostingAuditTrailRepository
{
    private readonly JobPostingsDbContext _dbContext;
    public PostingAuditTrailRepository(JobPostingsDbContext dbContext) => _dbContext = dbContext;

    public Task<PostingAuditTrail?> GetByPostingIdAsync(Guid jobPostingId, CancellationToken cancellationToken) =>
        _dbContext.PostingAuditTrails.Include(x => x.Entries).FirstOrDefaultAsync(x => x.JobPostingId == jobPostingId, cancellationToken);

    public Task AddAsync(PostingAuditTrail trail, CancellationToken cancellationToken) =>
        _dbContext.PostingAuditTrails.AddAsync(trail, cancellationToken).AsTask();
}

public sealed class EmployerStandingStore : IEmployerStandingStore
{
    private readonly JobPostingsDbContext _dbContext;
    public EmployerStandingStore(JobPostingsDbContext dbContext) => _dbContext = dbContext;

    public Task<EmployerStanding?> GetAsync(Guid employerId, CancellationToken cancellationToken) =>
        _dbContext.EmployerStandings.FirstOrDefaultAsync(x => x.EmployerId == employerId, cancellationToken);

    public async Task UpsertAsync(EmployerStanding standing, CancellationToken cancellationToken)
    {
        var existing = await GetAsync(standing.EmployerId, cancellationToken);
        if (existing is null)
        {
            await _dbContext.EmployerStandings.AddAsync(standing, cancellationToken);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(standing);
        }
    }
}

public sealed class PostingMetricsStore : IPostingMetricsStore
{
    private readonly JobPostingsDbContext _dbContext;
    public PostingMetricsStore(JobPostingsDbContext dbContext) => _dbContext = dbContext;
    public Task<PostingMetrics?> GetAsync(Guid jobPostingId, CancellationToken cancellationToken) =>
        _dbContext.PostingMetrics.FirstOrDefaultAsync(x => x.JobPostingId == jobPostingId, cancellationToken);

    public async Task UpsertAsync(PostingMetrics metrics, CancellationToken cancellationToken)
    {
        var existing = await GetAsync(metrics.JobPostingId, cancellationToken);
        if (existing is null)
        {
            await _dbContext.PostingMetrics.AddAsync(metrics, cancellationToken);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(metrics);
        }
    }
}

public sealed class JobPostingsUnitOfWork : IJobPostingsUnitOfWork
{
    private readonly JobPostingsDbContext _dbContext;
    public JobPostingsUnitOfWork(JobPostingsDbContext dbContext) => _dbContext = dbContext;
    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
