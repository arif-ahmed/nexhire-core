using Nexhire.Modules.Users.Core.Domain;

namespace Nexhire.Modules.Users.Core.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
