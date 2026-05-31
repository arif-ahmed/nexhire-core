// I will just stub PagedResult if not available.
// Let's assume Nexhire.Shared.Core.Responses.PagedResult<T> exists.

namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(UserAccountId id, CancellationToken ct = default);
    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<UserAccount?> GetByMobileAsync(string mobile, CancellationToken ct = default);
    Task<UserAccount?> GetByEmailOrMobileAsync(string identifier, CancellationToken ct = default);
    Task<UserAccount?> GetBySessionRefreshTokenHashAsync(string hash, CancellationToken ct = default);
    Task<UserAccount?> GetByPasswordResetTokenHashAsync(string hash, CancellationToken ct = default);
    Task<object> SearchAsync(object criteria, CancellationToken ct = default); // Stubbing PagedResult and UserSearchCriteria for now
    Task AddAsync(UserAccount user, CancellationToken ct = default);
    Task UpdateAsync(UserAccount user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> MobileExistsAsync(string mobile, CancellationToken ct = default);
}
