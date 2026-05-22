using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class PostingMatchProfileRepository : IPostingMatchProfileRepository
{
    private readonly RecommendationEngineDbContext _db;

    public PostingMatchProfileRepository(RecommendationEngineDbContext db) => _db = db;

    public async Task<PostingMatchProfile?> GetByIdAsync(PostingMatchProfileId id, CancellationToken cancellationToken)
        => await _db.PostingMatchProfiles.FindAsync([id.Value], cancellationToken);

    public async Task<PostingMatchProfile?> GetByPostingIdAsync(Guid postingId, CancellationToken cancellationToken)
        => await _db.PostingMatchProfiles
            .FirstOrDefaultAsync(p => p.JobPostingId == postingId, cancellationToken);

    public async Task<List<PostingMatchProfile>> GetActivePostingsAsync(CancellationToken cancellationToken)
        => await _db.PostingMatchProfiles
            .Where(p => p.Status == PostingMatchStatus.Active)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PostingMatchProfile profile, CancellationToken cancellationToken)
        => await _db.PostingMatchProfiles.AddAsync(profile, cancellationToken);
}
