using Nexhire.Modules.RecommendationEngine.Core.Domain.Ports;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Adapters;

public sealed class StubEmployerAccessApi : IEmployerAccessApi
{
    public Task<bool> HasAccessAsync(Guid employerId, Guid recruiterId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
