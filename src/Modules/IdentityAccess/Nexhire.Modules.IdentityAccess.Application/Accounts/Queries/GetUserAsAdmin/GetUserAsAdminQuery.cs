using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetUserAsAdmin;

public record GetUserAsAdminQuery(Guid AdminUserId, Guid TargetUserId) : IQuery<AdminUserDetailDto>;
