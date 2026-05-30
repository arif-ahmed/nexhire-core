using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Responses;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetAdminActionLog;

public record GetAdminActionLogQuery(
    Guid? AdminUserId,
    Guid? TargetUserId,
    string? ActionType,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<AdminActionDto>>;
