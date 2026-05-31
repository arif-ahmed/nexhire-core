namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;

public interface IRevokedTokenStore
{
    Task AddAsync(string tokenIdOrHash, DateTime expiresOnUtc, CancellationToken ct = default);
    Task<bool> IsRevokedAsync(string tokenIdOrHash, CancellationToken ct = default);
}
