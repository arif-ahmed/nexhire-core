using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetAdminActionLog;
using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.Responses;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Repositories;

public class AdminActionLogRepository : IAdminActionLogRepository
{
    private readonly IdentityAccessDbContext _dbContext;

    public AdminActionLogRepository(IdentityAccessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AdminActionLog log, CancellationToken ct = default)
    {
        await _dbContext.AdminActionLogs.AddAsync(log, ct);
    }

    public async Task<object> QueryAsync(object criteriaObj, CancellationToken ct = default)
    {
        var criteria = (GetAdminActionLogQuery)criteriaObj;
        var query = _dbContext.AdminActionLogs.AsQueryable();

        if (criteria.AdminUserId.HasValue)
        {
            query = query.Where(x => x.AdminUserId == criteria.AdminUserId.Value);
        }

        if (criteria.TargetUserId.HasValue)
        {
            query = query.Where(x => x.TargetUserId == criteria.TargetUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ActionType))
        {
            if (Enum.TryParse<AdminActionType>(criteria.ActionType, true, out var type))
            {
                query = query.Where(x => x.ActionType == type);
            }
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.OccurredOnUtc)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(x => new AdminActionDto(
                x.Id,
                x.AdminUserId,
                x.ActionType.ToString(),
                x.TargetUserId,
                x.Reason,
                x.OccurredOnUtc
            ))
            .ToListAsync(ct);

        return new PagedResult<AdminActionDto>(items, totalCount, criteria.Page, criteria.PageSize);
    }
}
