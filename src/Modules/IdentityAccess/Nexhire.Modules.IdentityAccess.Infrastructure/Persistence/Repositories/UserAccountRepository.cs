using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.ListUsers;
using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.Responses;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly IdentityAccessDbContext _dbContext;

    public UserAccountRepository(IdentityAccessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserAccount?> GetByIdAsync(UserAccountId id, CancellationToken ct = default)
    {
        return await _dbContext.UserAccounts
            .Include(u => u.Sessions)
            .Include(u => u.BackupCodes)
            .Include(u => u.PasswordResetTokens)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<UserAccount?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _dbContext.UserAccounts
            .Include(u => u.Sessions)
            .Include(u => u.BackupCodes)
            .Include(u => u.PasswordResetTokens)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Credential.Email.Value == email, ct);
    }

    public async Task<UserAccount?> GetByMobileAsync(string mobile, CancellationToken ct = default)
    {
        return await _dbContext.UserAccounts
            .Include(u => u.Sessions)
            .Include(u => u.BackupCodes)
            .Include(u => u.PasswordResetTokens)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Credential.Mobile != null && u.Credential.Mobile.Value == mobile, ct);
    }

    public async Task<UserAccount?> GetByEmailOrMobileAsync(string identifier, CancellationToken ct = default)
    {
        return await _dbContext.UserAccounts
            .Include(u => u.Sessions)
            .Include(u => u.BackupCodes)
            .Include(u => u.PasswordResetTokens)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Credential.Email.Value == identifier || (u.Credential.Mobile != null && u.Credential.Mobile.Value == identifier), ct);
    }

    public async Task<UserAccount?> GetBySessionRefreshTokenHashAsync(string hash, CancellationToken ct = default)
    {
        var userAccountId = await _dbContext.UserAccounts
            .SelectMany(u => u.Sessions, (u, s) => new { u.Id, s.RefreshTokenHash })
            .Where(x => x.RefreshTokenHash == hash)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (userAccountId == null) return null;

        return await GetByIdAsync(userAccountId, ct);
    }

    public async Task<UserAccount?> GetByPasswordResetTokenHashAsync(string hash, CancellationToken ct = default)
    {
        var userAccountId = await _dbContext.UserAccounts
            .SelectMany(u => u.PasswordResetTokens, (u, p) => new { u.Id, p.TokenHash })
            .Where(x => x.TokenHash == hash)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (userAccountId == null) return null;

        return await GetByIdAsync(userAccountId, ct);
    }

    public async Task<object> SearchAsync(object criteriaObj, CancellationToken ct = default)
    {
        var criteria = (ListUsersQuery)criteriaObj;
        var query = _dbContext.UserAccounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var searchTerm = criteria.SearchTerm.ToLower();
            query = query.Where(u => u.Credential.Email.Value.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Role))
        {
            if (Enum.TryParse<UserRole>(criteria.Role, true, out var role))
            {
                query = query.Where(u => u.Role == role);
            }
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            if (Enum.TryParse<AccountStatus>(criteria.Status, true, out var status))
            {
                query = query.Where(u => u.Status == status);
            }
        }

        var totalCount = await query.CountAsync(ct);
        
        var items = await query
            .OrderByDescending(u => u.CreatedOnUtc)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(u => new UserListItemDto(
                u.Id.Value,
                u.Credential.Email.Value,
                u.Role.ToString(),
                u.Status.ToString(),
                u.CreatedOnUtc,
                false // IdentityVerified
            ))
            .ToListAsync(ct);

        return new PagedResult<UserListItemDto>(items, totalCount, criteria.Page, criteria.PageSize);
    }

    public async Task AddAsync(UserAccount user, CancellationToken ct = default)
    {
        await _dbContext.UserAccounts.AddAsync(user, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        return await _dbContext.UserAccounts.AnyAsync(u => u.Credential.Email.Value == email, ct);
    }

    public async Task<bool> MobileExistsAsync(string mobile, CancellationToken ct = default)
    {
        return await _dbContext.UserAccounts.AnyAsync(u => u.Credential.Mobile != null && u.Credential.Mobile.Value == mobile, ct);
    }
}
