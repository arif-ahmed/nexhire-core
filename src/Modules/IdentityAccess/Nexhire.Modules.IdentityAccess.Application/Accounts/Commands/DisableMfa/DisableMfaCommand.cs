using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.DisableMfa;

public record DisableMfaCommand(Guid UserId) : ICommand;
