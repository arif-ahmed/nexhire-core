using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Repositories;

public class SearchSessionRepository : ISearchSessionRepository
{
    private readonly SearchDiscoveryDbContext _db;

    public SearchSessionRepository(SearchDiscoveryDbContext db) => _db = db;

    public async Task<SearchSession?> GetBySeekerAsync(Guid seekerId, CancellationToken ct = default)
        => await _db.SearchSessions.FirstOrDefaultAsync(s => s.SeekerUserId == seekerId, ct);

    public async Task AddAsync(SearchSession session, CancellationToken ct = default)
        => await _db.SearchSessions.AddAsync(session, ct);

    public async Task UpdateAsync(SearchSession session, CancellationToken ct = default)
        => _db.SearchSessions.Update(session);

    public async Task DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default)
        => await _db.SearchSessions.Where(s => s.ExpiresOnUtc < cutoff).ExecuteDeleteAsync(ct);
}
