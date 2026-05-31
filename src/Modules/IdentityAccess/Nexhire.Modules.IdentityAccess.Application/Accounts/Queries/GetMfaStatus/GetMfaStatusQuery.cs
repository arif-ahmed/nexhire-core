using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetMfaStatus;

public record GetMfaStatusQuery(Guid UserId) : IQuery<MfaStatusDto>;
