using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;
using ApplicationId = Nexhire.Modules.JobApplication.Core.Domain.ApplicationId;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence.Repositories;

public sealed class ApplicationRepository : IApplicationRepository
{
    private readonly JobApplicationDbContext _dbContext;

    public ApplicationRepository(JobApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Application?> GetByIdAsync(ApplicationId id, CancellationToken cancellationToken)
    {
        return await _dbContext.Applications
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Application?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken)
    {
        return await _dbContext.Applications
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<Application?> GetNonTerminalForAsync(Guid seekerId, Guid postingId, CancellationToken cancellationToken)
    {
        // Non-terminal states: Submitted, UnderReview, Shortlisted, Interview, Offered
        var nonTerminalStatuses = new[]
        {
            ApplicationStatus.Submitted,
            ApplicationStatus.UnderReview,
            ApplicationStatus.Shortlisted,
            ApplicationStatus.Interview,
            ApplicationStatus.Offered
        };

        return await _dbContext.Applications
            .FirstOrDefaultAsync(x => x.JobSeekerId == seekerId && 
                                      x.JobPostingId == postingId && 
                                      nonTerminalStatuses.Contains(x.Status), 
                                 cancellationToken);
    }

    public async Task<IReadOnlyCollection<Application>> GetTerminalForAsync(Guid seekerId, Guid postingId, CancellationToken cancellationToken)
    {
        // Terminal states: Hired, Rejected, Withdrawn, Expired
        var terminalStatuses = new[]
        {
            ApplicationStatus.Hired,
            ApplicationStatus.Rejected,
            ApplicationStatus.Withdrawn,
            ApplicationStatus.Expired
        };

        var list = await _dbContext.Applications
            .Where(x => x.JobSeekerId == seekerId && 
                        x.JobPostingId == postingId && 
                        terminalStatuses.Contains(x.Status))
            .ToListAsync(cancellationToken);

        return list.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Application>> GetNonTerminalByPostingAsync(Guid postingId, CancellationToken cancellationToken)
    {
        var nonTerminalStatuses = new[]
        {
            ApplicationStatus.Submitted,
            ApplicationStatus.UnderReview,
            ApplicationStatus.Shortlisted,
            ApplicationStatus.Interview,
            ApplicationStatus.Offered
        };

        var list = await _dbContext.Applications
            .Where(x => x.JobPostingId == postingId && nonTerminalStatuses.Contains(x.Status))
            .ToListAsync(cancellationToken);

        return list.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Application>> GetNonTerminalBySeekerAsync(Guid seekerId, CancellationToken cancellationToken)
    {
        var nonTerminalStatuses = new[]
        {
            ApplicationStatus.Submitted,
            ApplicationStatus.UnderReview,
            ApplicationStatus.Shortlisted,
            ApplicationStatus.Interview,
            ApplicationStatus.Offered
        };

        var list = await _dbContext.Applications
            .Where(x => x.JobSeekerId == seekerId && nonTerminalStatuses.Contains(x.Status))
            .ToListAsync(cancellationToken);

        return list.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Application>> ListBySeekerAsync(Guid seekerId, CancellationToken cancellationToken)
    {
        var list = await _dbContext.Applications
            .Where(x => x.JobSeekerId == seekerId)
            .ToListAsync(cancellationToken);

        return list.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Application>> ListByPostingAsync(Guid postingId, CancellationToken cancellationToken)
    {
        var list = await _dbContext.Applications
            .Where(x => x.JobPostingId == postingId)
            .ToListAsync(cancellationToken);

        return list.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Application>> ListByPostingsAsync(IEnumerable<Guid> postingIds, CancellationToken cancellationToken)
    {
        var postingIdsList = postingIds.ToList();
        var list = await _dbContext.Applications
            .Where(x => postingIdsList.Contains(x.JobPostingId))
            .ToListAsync(cancellationToken);

        return list.AsReadOnly();
    }

    public async Task AddAsync(Application application, CancellationToken cancellationToken)
    {
        await _dbContext.Applications.AddAsync(application, cancellationToken);
    }

    public void Update(Application application)
    {
        _dbContext.Applications.Update(application);
    }
}
