using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IRecommendationEngineUnitOfWork
{
    private readonly RecommendationEngineDbContext _db;

    public UnitOfWork(RecommendationEngineDbContext db) => _db = db;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _db.SaveChangesAsync(cancellationToken);
}
