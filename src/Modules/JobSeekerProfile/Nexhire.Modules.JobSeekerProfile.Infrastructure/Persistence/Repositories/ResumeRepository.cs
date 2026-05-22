using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Repositories;

public class ResumeRepository : IResumeRepository
{
    private readonly JobSeekerProfileDbContext _dbContext;

    public ResumeRepository(JobSeekerProfileDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Resume?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Resume?> GetActiveByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Resumes
            .FirstOrDefaultAsync(r => r.ProfileId == profileId && !r.IsSuperseded, cancellationToken);
    }

    public async Task AddAsync(Resume resume, CancellationToken cancellationToken = default)
    {
        await _dbContext.Resumes.AddAsync(resume, cancellationToken);
    }

    public Task UpdateAsync(Resume resume, CancellationToken cancellationToken = default)
    {
        _dbContext.Resumes.Update(resume);
        return Task.CompletedTask;
    }
}
