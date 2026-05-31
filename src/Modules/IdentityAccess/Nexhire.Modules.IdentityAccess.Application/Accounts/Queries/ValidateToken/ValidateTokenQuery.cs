using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.ValidateToken;

public record ValidateTokenQuery(string AccessToken) : IQuery<ValidatedPrincipal>;
