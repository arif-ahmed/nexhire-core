using Nexhire.Modules.SearchDiscovery.Core.Domain.Ports;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.Adapters;

public class StubRecommendationQueryApi : IRecommendationQueryApi
{
    public Task<Result<IReadOnlyDictionary<Guid, int>>> GetMatchScoresAsync(Guid jobSeekerId, IReadOnlyList<Guid> postingIds, CancellationToken ct = default)
        => Task.FromResult(Result.Success<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>()));

    public Task<Result<IReadOnlyList<Guid>>> GetRecommendedPostingIdsAsync(Guid jobSeekerId, int maxItems, CancellationToken ct = default)
        => Task.FromResult(Result.Success<IReadOnlyList<Guid>>(new List<Guid>()));
}
