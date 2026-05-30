using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.RevokeToken;

public record RevokeTokenCommand(string Token) : ICommand;
