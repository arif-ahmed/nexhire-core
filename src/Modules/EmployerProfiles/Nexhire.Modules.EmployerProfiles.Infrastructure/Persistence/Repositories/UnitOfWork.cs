using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly EmployerProfilesDbContext _dbContext;

    public UnitOfWork(EmployerProfilesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
