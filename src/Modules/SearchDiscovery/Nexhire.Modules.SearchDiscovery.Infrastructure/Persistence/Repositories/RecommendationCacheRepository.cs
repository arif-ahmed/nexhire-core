using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Persistence.Repositories;

public class RecommendationCacheRepository : IRecommendationCacheRepository
{
    public Task<IReadOnlyList<Guid>?> GetAsync(Guid seekerId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Guid>?>(null);

    public Task ReplaceAsync(Guid seekerId, IReadOnlyList<Guid> postingIds, DateTime computedAtUtc, CancellationToken ct = default)
        => Task.CompletedTask;
}
