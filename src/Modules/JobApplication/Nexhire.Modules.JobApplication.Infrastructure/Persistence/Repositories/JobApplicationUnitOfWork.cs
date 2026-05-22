using Nexhire.Modules.JobApplication.Core.Domain.Repositories;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence.Repositories;

public sealed class JobApplicationUnitOfWork : IJobApplicationUnitOfWork
{
    private readonly JobApplicationDbContext _dbContext;

    public JobApplicationUnitOfWork(JobApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
