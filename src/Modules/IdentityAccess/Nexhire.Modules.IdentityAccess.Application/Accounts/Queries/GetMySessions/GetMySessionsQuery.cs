using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetMySessions;

public record GetMySessionsQuery(Guid UserId, Guid CurrentSessionId) : IQuery<IReadOnlyList<SessionDto>>;
