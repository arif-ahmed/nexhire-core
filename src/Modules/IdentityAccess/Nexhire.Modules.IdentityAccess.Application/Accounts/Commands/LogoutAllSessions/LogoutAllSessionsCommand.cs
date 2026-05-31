using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.LogoutAllSessions;

public record LogoutAllSessionsCommand(Guid UserId) : ICommand;
