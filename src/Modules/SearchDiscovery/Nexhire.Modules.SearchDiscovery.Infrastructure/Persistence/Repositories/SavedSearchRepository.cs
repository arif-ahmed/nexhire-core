using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Repositories;

public class SavedSearchRepository : ISavedSearchRepository
{
    private readonly SearchDiscoveryDbContext _db;

    public SavedSearchRepository(SearchDiscoveryDbContext db) => _db = db;

    public async Task<SavedSearch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.SavedSearches.FindAsync([id], ct);

    public async Task<IReadOnlyList<SavedSearch>> ListBySeekerAsync(Guid seekerId, CancellationToken ct = default)
        => await _db.SavedSearches.Where(s => s.SeekerUserId == seekerId && !s.IsDeleted).ToListAsync(ct);

    public async Task<bool> IsNameTakenAsync(Guid seekerId, string name, Guid? excludeId = null, CancellationToken ct = default)
        => await _db.SavedSearches.AnyAsync(s => s.SeekerUserId == seekerId && s.Name == name && !s.IsDeleted && (excludeId == null || s.Id != excludeId), ct);

    public async Task<IReadOnlyList<SavedSearch>> ListActiveForEvaluationAsync(CancellationToken ct = default)
        => await _db.SavedSearches
            .Where(s => !s.IsDeleted && s.NotificationPreference != NotificationPreference.None)
            .ToListAsync(ct);

    public async Task AddAsync(SavedSearch savedSearch, CancellationToken ct = default)
        => await _db.SavedSearches.AddAsync(savedSearch, ct);

    public async Task UpdateAsync(SavedSearch savedSearch, CancellationToken ct = default)
        => _db.SavedSearches.Update(savedSearch);

    public async Task DeleteAsync(SavedSearch savedSearch, CancellationToken ct = default)
        => _db.SavedSearches.Remove(savedSearch);
}
