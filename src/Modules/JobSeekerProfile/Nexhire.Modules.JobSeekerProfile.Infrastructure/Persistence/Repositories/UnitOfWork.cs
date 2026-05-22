using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly JobSeekerProfileDbContext _dbContext;

    public UnitOfWork(JobSeekerProfileDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
