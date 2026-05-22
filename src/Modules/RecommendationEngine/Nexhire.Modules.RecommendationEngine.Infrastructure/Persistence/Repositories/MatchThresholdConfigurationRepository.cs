using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class MatchThresholdConfigurationRepository : IMatchThresholdConfigurationRepository
{
    private readonly RecommendationEngineDbContext _db;

    public MatchThresholdConfigurationRepository(RecommendationEngineDbContext db) => _db = db;

    public async Task<MatchThresholdConfiguration?> GetDefaultAsync(CancellationToken cancellationToken)
        => await _db.MatchThresholdConfigurations.FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(MatchThresholdConfiguration config, CancellationToken cancellationToken)
        => await _db.MatchThresholdConfigurations.AddAsync(config, cancellationToken);

    public Task UpdateAsync(MatchThresholdConfiguration config, CancellationToken cancellationToken)
    {
        _db.MatchThresholdConfigurations.Update(config);
        return Task.CompletedTask;
    }
}
