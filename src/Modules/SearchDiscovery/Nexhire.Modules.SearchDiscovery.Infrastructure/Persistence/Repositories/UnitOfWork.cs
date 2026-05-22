using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SearchDiscoveryDbContext _db;

    public UnitOfWork(SearchDiscoveryDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
