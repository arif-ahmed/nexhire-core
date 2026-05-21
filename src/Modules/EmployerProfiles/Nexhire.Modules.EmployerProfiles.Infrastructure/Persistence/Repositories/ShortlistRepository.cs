using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Repositories;

public class ShortlistRepository : IShortlistRepository
{
    private readonly EmployerProfilesDbContext _dbContext;

    public ShortlistRepository(EmployerProfilesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Shortlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Shortlists
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Shortlist>> GetByEmployerProfileIdAsync(Guid employerProfileId, CancellationToken cancellationToken = default)
    {
        var shortlists = await _dbContext.Shortlists
            .Include(s => s.Members)
            .Where(s => s.EmployerProfileId == employerProfileId && !s.IsDeleted)
            .ToListAsync(cancellationToken);
            
        return shortlists.AsReadOnly();
    }

    public async Task AddAsync(Shortlist shortlist, CancellationToken cancellationToken = default)
    {
        await _dbContext.Shortlists.AddAsync(shortlist, cancellationToken);
    }

    public Task UpdateAsync(Shortlist shortlist, CancellationToken cancellationToken = default)
    {
        _dbContext.Shortlists.Update(shortlist);
        return Task.CompletedTask;
    }
}
