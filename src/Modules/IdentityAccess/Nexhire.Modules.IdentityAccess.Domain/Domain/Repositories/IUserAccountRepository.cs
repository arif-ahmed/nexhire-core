using Nexhire.Modules.IdentityAccess.Domain;

namespace Nexhire.Modules.IdentityAccess.Domain.Repositories;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(UserAccount user, CancellationToken cancellationToken = default);
}
