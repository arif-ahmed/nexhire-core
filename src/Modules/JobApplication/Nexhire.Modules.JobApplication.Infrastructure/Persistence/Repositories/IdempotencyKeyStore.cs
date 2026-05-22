using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;
using ApplicationId = Nexhire.Modules.JobApplication.Core.Domain.ApplicationId;

namespace Nexhire.Modules.JobApplication.Infrastructure.Persistence.Repositories;

public sealed class IdempotencyKeyStore : IIdempotencyKeyStore
{
    private readonly JobApplicationDbContext _dbContext;

    public IdempotencyKeyStore(JobApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> TryGetAsync(Guid idempotencyKey, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.IdempotencyKeys
            .FirstOrDefaultAsync(x => x.Key == idempotencyKey, cancellationToken);
        
        return entry?.ApplicationId;
    }

    public async Task SaveAsync(Guid idempotencyKey, ApplicationId applicationId, CancellationToken cancellationToken)
    {
        var entry = new IdempotencyKeyEntry
        {
            Key = idempotencyKey,
            ApplicationId = applicationId.Value,
            CreatedOnUtc = DateTime.UtcNow
        };

        await _dbContext.IdempotencyKeys.AddAsync(entry, cancellationToken);
    }

    public async Task PurgeOlderThanAsync(DateTime threshold, CancellationToken cancellationToken)
    {
        var oldKeys = await _dbContext.IdempotencyKeys
            .Where(x => x.CreatedOnUtc < threshold)
            .ToListAsync(cancellationToken);

        if (oldKeys.Count > 0)
        {
            _dbContext.IdempotencyKeys.RemoveRange(oldKeys);
        }
    }
}
