using Nexhire.Modules.RecommendationEngine.Core.Domain.Ports;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Adapters;

public sealed class StubCollaborativeFilteringPort : ICollaborativeFilteringPort
{
    public Task<List<Guid>> GetCollaborativeRecommendationsAsync(Guid jobSeekerId, int limit = 20, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<Guid>());
    }
}
