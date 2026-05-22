using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;

public interface IJobIndexEntryRepository
{
    Task<JobIndexEntry?> GetByIdAsync(Guid postingId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid postingId, CancellationToken ct = default);
    Task<IReadOnlyList<JobIndexEntry>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default);
    Task<int> CountAsync(SearchCriteria criteria, CancellationToken ct = default);
    Task AddAsync(JobIndexEntry entry, CancellationToken ct = default);
    Task UpdateAsync(JobIndexEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid postingId, CancellationToken ct = default);
}

public interface IFavoriteJobRepository
{
    Task<FavoriteJob?> GetBySeekerAndPostingAsync(Guid seekerId, Guid postingId, CancellationToken ct = default);
    Task<IReadOnlyList<FavoriteJob>> ListBySeekerAsync(Guid seekerId, CancellationToken ct = default);
    Task AddAsync(FavoriteJob favorite, CancellationToken ct = default);
    Task DeleteAsync(FavoriteJob favorite, CancellationToken ct = default);
    Task<bool> IsFavoritedAsync(Guid seekerId, Guid postingId, CancellationToken ct = default);
}

public interface ISavedSearchRepository
{
    Task<SavedSearch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SavedSearch>> ListBySeekerAsync(Guid seekerId, CancellationToken ct = default);
    Task<bool> IsNameTakenAsync(Guid seekerId, string name, Guid? excludeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<SavedSearch>> ListActiveForEvaluationAsync(CancellationToken ct = default);
    Task AddAsync(SavedSearch savedSearch, CancellationToken ct = default);
    Task UpdateAsync(SavedSearch savedSearch, CancellationToken ct = default);
    Task DeleteAsync(SavedSearch savedSearch, CancellationToken ct = default);
}

public interface ISearchSessionRepository
{
    Task<SearchSession?> GetBySeekerAsync(Guid seekerId, CancellationToken ct = default);
    Task AddAsync(SearchSession session, CancellationToken ct = default);
    Task UpdateAsync(SearchSession session, CancellationToken ct = default);
    Task DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default);
}

public interface IMatchScoreCacheRepository
{
    Task<IReadOnlyDictionary<Guid, int>> GetScoresAsync(Guid seekerId, IEnumerable<Guid> postingIds, CancellationToken ct = default);
    Task UpsertAsync(Guid seekerId, Guid postingId, int score, DateTime computedOnUtc, CancellationToken ct = default);
}

public interface IRecommendationCacheRepository
{
    Task<IReadOnlyList<Guid>?> GetAsync(Guid seekerId, CancellationToken ct = default);
    Task ReplaceAsync(Guid seekerId, IReadOnlyList<Guid> postingIds, DateTime computedAtUtc, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
