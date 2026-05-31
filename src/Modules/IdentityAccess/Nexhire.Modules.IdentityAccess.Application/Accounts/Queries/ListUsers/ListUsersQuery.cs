using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Responses;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.ListUsers;

public record ListUsersQuery(
    string? SearchTerm,
    string? Role,
    string? Status,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<UserListItemDto>>;
