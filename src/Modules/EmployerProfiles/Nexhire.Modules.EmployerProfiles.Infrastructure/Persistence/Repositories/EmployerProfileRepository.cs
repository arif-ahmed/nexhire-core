using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Repositories;

public class EmployerProfileRepository : IEmployerProfileRepository
{
    private readonly EmployerProfilesDbContext _dbContext;

    public EmployerProfileRepository(EmployerProfilesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmployerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmployerProfiles
            .Include(ep => ep.Images)
            .Include(ep => ep.Documents)
            .FirstOrDefaultAsync(ep => ep.Id == id, cancellationToken);
    }

    public async Task<EmployerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmployerProfiles
            .Include(ep => ep.Images)
            .Include(ep => ep.Documents)
            .FirstOrDefaultAsync(ep => ep.UserId == userId, cancellationToken);
    }

    public async Task<bool> CompanyIdentifierExistsAsync(string companyIdentifier, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmployerProfiles
            .AnyAsync(ep => ep.CompanyIdentifier.Value == companyIdentifier, cancellationToken);
    }

    public async Task AddAsync(EmployerProfile profile, CancellationToken cancellationToken = default)
    {
        await _dbContext.EmployerProfiles.AddAsync(profile, cancellationToken);
    }

    public Task UpdateAsync(EmployerProfile profile, CancellationToken cancellationToken = default)
    {
        _dbContext.EmployerProfiles.Update(profile);
        return Task.CompletedTask;
    }
}
