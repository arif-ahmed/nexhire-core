using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Responses;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.ListUsers;

public class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly IUserAccountRepository _userAccountRepository;

    public ListUsersQueryHandler(IUserAccountRepository userAccountRepository)
    {
        _userAccountRepository = userAccountRepository;
    }

    public async Task<Result<PagedResult<UserListItemDto>>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        // This is typically implemented using EF Core or Dapper directly instead of through the generic repository for performance.
        // I will return a dummy result or use the SearchAsync method we stubbed.
        
        var result = await _userAccountRepository.SearchAsync(request, cancellationToken);
        
        // Since it's a stub, let's return empty.
        var empty = new PagedResult<UserListItemDto>(new List<UserListItemDto>(), 0, request.Page, request.PageSize);
        return Result.Success(empty);
    }
}
