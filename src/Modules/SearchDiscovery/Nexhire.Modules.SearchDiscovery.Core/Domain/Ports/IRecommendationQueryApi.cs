using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Ports;

public interface IRecommendationQueryApi
{
    Task<Result<IReadOnlyDictionary<Guid, int>>> GetMatchScoresAsync(Guid jobSeekerId, IReadOnlyList<Guid> postingIds, CancellationToken ct = default);
    Task<Result<IReadOnlyList<Guid>>> GetRecommendedPostingIdsAsync(Guid jobSeekerId, int maxItems, CancellationToken ct = default);
}
