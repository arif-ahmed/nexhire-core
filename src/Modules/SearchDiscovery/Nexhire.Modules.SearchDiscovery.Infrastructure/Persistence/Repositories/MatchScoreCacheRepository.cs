using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Repositories;

public class MatchScoreCacheRepository : IMatchScoreCacheRepository
{
    public Task<IReadOnlyDictionary<Guid, int>> GetScoresAsync(Guid seekerId, IEnumerable<Guid> postingIds, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());

    public Task UpsertAsync(Guid seekerId, Guid postingId, int score, DateTime computedOnUtc, CancellationToken ct = default)
        => Task.CompletedTask;
}
