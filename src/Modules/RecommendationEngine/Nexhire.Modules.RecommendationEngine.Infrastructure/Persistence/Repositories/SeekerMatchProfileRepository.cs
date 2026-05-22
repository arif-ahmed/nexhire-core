using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class SeekerMatchProfileRepository : ISeekerMatchProfileRepository
{
    private readonly RecommendationEngineDbContext _db;

    public SeekerMatchProfileRepository(RecommendationEngineDbContext db) => _db = db;

    public async Task<SeekerMatchProfile?> GetByIdAsync(SeekerMatchProfileId id, CancellationToken cancellationToken)
        => await _db.SeekerMatchProfiles.FindAsync([id.Value], cancellationToken);

    public async Task<SeekerMatchProfile?> GetBySeekerIdAsync(Guid seekerId, CancellationToken cancellationToken)
        => await _db.SeekerMatchProfiles
            .FirstOrDefaultAsync(p => p.JobSeekerId == seekerId, cancellationToken);

    public async Task<List<SeekerMatchProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken)
        => await _db.SeekerMatchProfiles
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(SeekerMatchProfile profile, CancellationToken cancellationToken)
        => await _db.SeekerMatchProfiles.AddAsync(profile, cancellationToken);
}
