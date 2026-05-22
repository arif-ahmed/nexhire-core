using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Persistence.Repositories;

public sealed class EmbeddingRecordRepository : IEmbeddingRecordRepository
{
    private readonly RecommendationEngineDbContext _db;

    public EmbeddingRecordRepository(RecommendationEngineDbContext db) => _db = db;

    public async Task<EmbeddingRecord?> GetByIdAsync(EmbeddingRecordId id, CancellationToken cancellationToken)
        => await _db.EmbeddingRecords.FindAsync([id.Value], cancellationToken);

    public async Task<EmbeddingRecord?> GetByOwnerIdAsync(Guid ownerId, EmbeddingOwnerType ownerType, CancellationToken cancellationToken)
        => await _db.EmbeddingRecords
            .FirstOrDefaultAsync(r => r.OwnerId == ownerId && r.OwnerType == ownerType, cancellationToken);

    public async Task<List<EmbeddingRecord>> GetPendingUploadsAsync(CancellationToken cancellationToken)
        => await _db.EmbeddingRecords
            .Where(r => r.Vector == null)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(EmbeddingRecord record, CancellationToken cancellationToken)
        => await _db.EmbeddingRecords.AddAsync(record, cancellationToken);

    public Task UpdateAsync(EmbeddingRecord record, CancellationToken cancellationToken)
    {
        _db.EmbeddingRecords.Update(record);
        return Task.CompletedTask;
    }
}
