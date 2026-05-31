using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Responses;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetAdminActionLog;

public class GetAdminActionLogQueryHandler : IQueryHandler<GetAdminActionLogQuery, PagedResult<AdminActionDto>>
{
    private readonly IAdminActionLogRepository _adminActionLogRepository;

    public GetAdminActionLogQueryHandler(IAdminActionLogRepository adminActionLogRepository)
    {
        _adminActionLogRepository = adminActionLogRepository;
    }

    public async Task<Result<PagedResult<AdminActionDto>>> Handle(GetAdminActionLogQuery request, CancellationToken cancellationToken)
    {
        var result = await _adminActionLogRepository.QueryAsync(request, cancellationToken);
        
        var empty = new PagedResult<AdminActionDto>(new List<AdminActionDto>(), 0, request.Page, request.PageSize);
        return Result.Success(empty);
    }
}
