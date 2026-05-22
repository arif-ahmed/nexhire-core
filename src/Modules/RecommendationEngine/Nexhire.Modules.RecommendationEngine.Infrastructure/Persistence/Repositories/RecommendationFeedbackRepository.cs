using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class RecommendationFeedbackRepository : IRecommendationFeedbackRepository
{
    private readonly RecommendationEngineDbContext _db;

    public RecommendationFeedbackRepository(RecommendationEngineDbContext db) => _db = db;

    public async Task<List<RecommendationFeedback>> GetFeedbackForSeekerAsync(Guid jobSeekerId, CancellationToken cancellationToken)
        => await _db.RecommendationFeedback
            .Where(f => f.JobSeekerId == jobSeekerId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(RecommendationFeedback feedback, CancellationToken cancellationToken)
        => await _db.RecommendationFeedback.AddAsync(feedback, cancellationToken);
}
