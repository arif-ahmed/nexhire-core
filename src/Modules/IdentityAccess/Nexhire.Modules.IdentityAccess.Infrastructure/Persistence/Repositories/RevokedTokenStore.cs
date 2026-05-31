using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Repositories;

public class RevokedTokenStore : IRevokedTokenStore
{
    private readonly IdentityAccessDbContext _dbContext;

    public RevokedTokenStore(IdentityAccessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(string tokenIdOrHash, DateTime expiresOnUtc, CancellationToken ct = default)
    {
        var token = new RevokedToken(tokenIdOrHash, DateTime.UtcNow, expiresOnUtc);
        await _dbContext.RevokedTokens.AddAsync(token, ct);
    }

    public async Task<bool> IsRevokedAsync(string tokenIdOrHash, CancellationToken ct = default)
    {
        return await _dbContext.RevokedTokens.AnyAsync(x => x.TokenIdOrRefreshHash == tokenIdOrHash, ct);
    }
}
