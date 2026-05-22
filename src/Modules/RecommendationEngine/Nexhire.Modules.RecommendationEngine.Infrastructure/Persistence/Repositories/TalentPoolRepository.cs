using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class TalentPoolRepository : ITalentPoolRepository
{
    private readonly RecommendationEngineDbContext _db;

    public TalentPoolRepository(RecommendationEngineDbContext db) => _db = db;

    public async Task<TalentPool?> GetByIdAsync(TalentPoolId id, CancellationToken cancellationToken)
        => await _db.TalentPools.FindAsync([id.Value], cancellationToken);

    public async Task<List<TalentPool>> GetByEmployerIdAsync(Guid employerId, CancellationToken cancellationToken)
        => await _db.TalentPools
            .Where(p => p.EmployerId == employerId)
            .ToListAsync(cancellationToken);

    public async Task<int> GetActivePoolCountForEmployerAsync(Guid employerId, CancellationToken cancellationToken)
        => await _db.TalentPools
            .CountAsync(p => p.EmployerId == employerId, cancellationToken);

    public async Task AddAsync(TalentPool pool, CancellationToken cancellationToken)
        => await _db.TalentPools.AddAsync(pool, cancellationToken);

    public Task UpdateAsync(TalentPool pool, CancellationToken cancellationToken)
    {
        _db.TalentPools.Update(pool);
        return Task.CompletedTask;
    }
}
