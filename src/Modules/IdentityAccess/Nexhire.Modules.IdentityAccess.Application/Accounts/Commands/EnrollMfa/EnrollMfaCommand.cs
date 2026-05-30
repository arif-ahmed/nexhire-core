using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.EnrollMfa;

public record EnrollMfaCommand(Guid UserId, string Method) : ICommand<EnrollMfaResultDto>;
