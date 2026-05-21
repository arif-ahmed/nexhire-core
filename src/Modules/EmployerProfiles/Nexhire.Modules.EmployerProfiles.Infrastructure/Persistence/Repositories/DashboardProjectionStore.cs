using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Repositories;

public class DashboardProjectionStore : IDashboardProjectionStore
{
    private readonly EmployerProfilesDbContext _dbContext;

    public DashboardProjectionStore(EmployerProfilesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertPostingAsync(DashboardPosting posting, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.DashboardPostings
            .FirstOrDefaultAsync(dp => dp.PostingId == posting.PostingId, cancellationToken);

        if (existing != null)
        {
            existing.Title = posting.Title;
            existing.Status = posting.Status;
            existing.LastEventOnUtc = posting.LastEventOnUtc;
            existing.EmployerUserId = posting.EmployerUserId;
            _dbContext.DashboardPostings.Update(existing);
        }
        else
        {
            await _dbContext.DashboardPostings.AddAsync(posting, cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePostingAsync(Guid postingId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.DashboardPostings
            .FirstOrDefaultAsync(dp => dp.PostingId == postingId, cancellationToken);

        if (existing != null)
        {
            _dbContext.DashboardPostings.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddApplicationAsync(DashboardApplication application, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.DashboardApplications
            .AnyAsync(da => da.ApplicationId == application.ApplicationId, cancellationToken);
            
        if (!exists)
        {
            await _dbContext.DashboardApplications.AddAsync(application, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpsertMatchedCandidateAsync(DashboardMatchedCandidate candidate, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.DashboardMatchedCandidates
            .FirstOrDefaultAsync(dmc => dmc.EmployerUserId == candidate.EmployerUserId 
                                     && dmc.PostingId == candidate.PostingId 
                                     && dmc.CandidateUserId == candidate.CandidateUserId, cancellationToken);

        if (existing != null)
        {
            existing.MatchScore = candidate.MatchScore;
            existing.GeneratedOnUtc = candidate.GeneratedOnUtc;
            _dbContext.DashboardMatchedCandidates.Update(existing);
        }
        else
        {
            await _dbContext.DashboardMatchedCandidates.AddAsync(candidate, cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardPosting>> GetPostingsAsync(Guid employerUserId, CancellationToken cancellationToken = default)
    {
        var postings = await _dbContext.DashboardPostings
            .Where(dp => dp.EmployerUserId == employerUserId)
            .ToListAsync(cancellationToken);
            
        return postings.AsReadOnly();
    }

    public async Task<IReadOnlyList<DashboardApplication>> GetApplicationsAsync(Guid employerUserId, CancellationToken cancellationToken = default)
    {
        var applications = await _dbContext.DashboardApplications
            .Where(da => da.EmployerUserId == employerUserId)
            .ToListAsync(cancellationToken);
            
        return applications.AsReadOnly();
    }

    public async Task<IReadOnlyList<DashboardMatchedCandidate>> GetMatchedCandidatesAsync(Guid employerUserId, CancellationToken cancellationToken = default)
    {
        var candidates = await _dbContext.DashboardMatchedCandidates
            .Where(dmc => dmc.EmployerUserId == employerUserId)
            .ToListAsync(cancellationToken);
            
        return candidates.AsReadOnly();
    }
}
