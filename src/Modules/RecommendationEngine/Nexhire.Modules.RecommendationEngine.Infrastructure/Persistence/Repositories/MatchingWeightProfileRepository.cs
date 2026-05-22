using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class MatchingWeightProfileRepository : IMatchingWeightProfileRepository
{
    private readonly RecommendationEngineDbContext _db;

    public MatchingWeightProfileRepository(RecommendationEngineDbContext db) => _db = db;

    public async Task<MatchingWeightProfile?> GetByIdAsync(MatchingWeightProfileId id, CancellationToken cancellationToken)
        => await _db.MatchingWeightProfiles.FindAsync([id.Value], cancellationToken);

    public async Task<MatchingWeightProfile?> GetByVersionAsync(string version, CancellationToken cancellationToken)
        => await _db.MatchingWeightProfiles
            .FirstOrDefaultAsync(p => p.Version == version, cancellationToken);

    public async Task<List<MatchingWeightProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken)
        => await _db.MatchingWeightProfiles
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<List<MatchingWeightProfile>> GetHistoricalVersionsAsync(string variantId, int limit, CancellationToken cancellationToken)
        => await _db.MatchingWeightProfiles
            .Where(p => p.VariantId == variantId)
            .OrderByDescending(p => p.Version)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(MatchingWeightProfile profile, CancellationToken cancellationToken)
        => await _db.MatchingWeightProfiles.AddAsync(profile, cancellationToken);

    public Task UpdateAsync(MatchingWeightProfile profile, CancellationToken cancellationToken)
    {
        _db.MatchingWeightProfiles.Update(profile);
        return Task.CompletedTask;
    }
}
