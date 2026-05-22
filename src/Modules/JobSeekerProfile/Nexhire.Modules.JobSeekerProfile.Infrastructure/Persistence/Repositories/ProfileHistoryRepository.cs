using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Repositories;

public class ProfileHistoryRepository : IProfileHistoryRepository
{
    private readonly JobSeekerProfileDbContext _dbContext;

    public ProfileHistoryRepository(JobSeekerProfileDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProfileHistory?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProfileHistories
            .Include(ph => ph.Versions)
            .FirstOrDefaultAsync(ph => ph.JobSeekerProfileId == profileId, cancellationToken);
    }

    public async Task AddAsync(ProfileHistory history, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProfileHistories.AddAsync(history, cancellationToken);
    }

    public Task UpdateAsync(ProfileHistory history, CancellationToken cancellationToken = default)
    {
        _dbContext.ProfileHistories.Update(history);
        return Task.CompletedTask;
    }
}
