using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class MatchScoreRepository : IMatchScoreRepository
{
    private readonly RecommendationEngineDbContext _db;

    public MatchScoreRepository(RecommendationEngineDbContext db) => _db = db;

    public async Task<MatchScore?> GetByIdAsync(MatchScoreId id, CancellationToken cancellationToken)
        => await _db.MatchScores.FindAsync([id.Value], cancellationToken);

    public async Task<MatchScore?> GetByKeysAsync(Guid jobSeekerId, Guid jobPostingId, CancellationToken cancellationToken)
        => await _db.MatchScores
            .FirstOrDefaultAsync(s => s.JobSeekerId == jobSeekerId && s.JobPostingId == jobPostingId, cancellationToken);

    public async Task<List<MatchScore>> GetByPostingIdAsync(Guid jobPostingId, CancellationToken cancellationToken)
        => await _db.MatchScores
            .Where(s => s.JobPostingId == jobPostingId)
            .ToListAsync(cancellationToken);

    public async Task<List<MatchScore>> GetBySeekerIdAsync(Guid jobSeekerId, CancellationToken cancellationToken)
        => await _db.MatchScores
            .Where(s => s.JobSeekerId == jobSeekerId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(MatchScore score, CancellationToken cancellationToken)
        => await _db.MatchScores.AddAsync(score, cancellationToken);

    public Task UpdateAsync(MatchScore score, CancellationToken cancellationToken)
    {
        _db.MatchScores.Update(score);
        return Task.CompletedTask;
    }
}
