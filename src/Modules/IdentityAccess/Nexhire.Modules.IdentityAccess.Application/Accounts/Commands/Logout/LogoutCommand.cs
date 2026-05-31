using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.Logout;

public record LogoutCommand(Guid UserId, Guid SessionId) : ICommand;
