using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Repositories;

public class FavoriteJobRepository : IFavoriteJobRepository
{
    private readonly SearchDiscoveryDbContext _db;

    public FavoriteJobRepository(SearchDiscoveryDbContext db) => _db = db;

    public async Task<FavoriteJob?> GetBySeekerAndPostingAsync(Guid seekerId, Guid postingId, CancellationToken ct = default)
        => await _db.FavoriteJobs.FirstOrDefaultAsync(f => f.SeekerUserId == seekerId && f.PostingId == postingId, ct);

    public async Task<IReadOnlyList<FavoriteJob>> ListBySeekerAsync(Guid seekerId, CancellationToken ct = default)
        => await _db.FavoriteJobs.Where(f => f.SeekerUserId == seekerId).ToListAsync(ct);

    public async Task AddAsync(FavoriteJob favorite, CancellationToken ct = default)
        => await _db.FavoriteJobs.AddAsync(favorite, ct);

    public async Task DeleteAsync(FavoriteJob favorite, CancellationToken ct = default)
        => _db.FavoriteJobs.Remove(favorite);

    public async Task<bool> IsFavoritedAsync(Guid seekerId, Guid postingId, CancellationToken ct = default)
        => await _db.FavoriteJobs.AnyAsync(f => f.SeekerUserId == seekerId && f.PostingId == postingId, ct);
}
