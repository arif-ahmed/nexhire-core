using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetMyAccount;

public record GetMyAccountQuery(Guid UserId) : IQuery<AccountDto>;
