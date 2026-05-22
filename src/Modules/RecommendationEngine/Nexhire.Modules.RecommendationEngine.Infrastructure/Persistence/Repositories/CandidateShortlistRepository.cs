using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class CandidateShortlistRepository : ICandidateShortlistRepository
{
    private readonly RecommendationEngineDbContext _db;

    public CandidateShortlistRepository(RecommendationEngineDbContext db) => _db = db;

    public async Task<CandidateShortlist?> GetByIdAsync(CandidateShortlistId id, CancellationToken cancellationToken)
        => await _db.CandidateShortlists.FindAsync([id.Value], cancellationToken);

    public async Task<CandidateShortlist?> GetByPostingIdAsync(Guid jobPostingId, CancellationToken cancellationToken)
        => await _db.CandidateShortlists
            .FirstOrDefaultAsync(s => s.JobPostingId == jobPostingId, cancellationToken);

    public async Task AddAsync(CandidateShortlist shortlist, CancellationToken cancellationToken)
        => await _db.CandidateShortlists.AddAsync(shortlist, cancellationToken);

    public Task UpdateAsync(CandidateShortlist shortlist, CancellationToken cancellationToken)
    {
        _db.CandidateShortlists.Update(shortlist);
        return Task.CompletedTask;
    }
}
