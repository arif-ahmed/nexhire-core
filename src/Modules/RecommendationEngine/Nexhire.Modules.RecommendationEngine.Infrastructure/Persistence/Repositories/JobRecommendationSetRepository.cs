using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class JobRecommendationSetRepository : IJobRecommendationSetRepository
{
    private readonly RecommendationEngineDbContext _db;

    public JobRecommendationSetRepository(RecommendationEngineDbContext db) => _db = db;

    public async Task<JobRecommendationSet?> GetByIdAsync(JobRecommendationSetId id, CancellationToken cancellationToken)
        => await _db.JobRecommendationSets.FindAsync([id.Value], cancellationToken);

    public async Task<JobRecommendationSet?> GetBySeekerIdAsync(Guid jobSeekerId, CancellationToken cancellationToken)
        => await _db.JobRecommendationSets
            .FirstOrDefaultAsync(s => s.JobSeekerId == jobSeekerId, cancellationToken);

    public async Task AddAsync(JobRecommendationSet set, CancellationToken cancellationToken)
        => await _db.JobRecommendationSets.AddAsync(set, cancellationToken);

    public Task UpdateAsync(JobRecommendationSet set, CancellationToken cancellationToken)
    {
        _db.JobRecommendationSets.Update(set);
        return Task.CompletedTask;
    }
}
