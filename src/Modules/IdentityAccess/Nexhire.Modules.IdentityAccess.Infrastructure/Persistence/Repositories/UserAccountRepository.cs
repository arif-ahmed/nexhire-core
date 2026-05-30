using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.IdentityAccess.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Repositories;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly IdentityAccessDbContext _dbContext;

    public UserAccountRepository(IdentityAccessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task AddAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserAccounts.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
