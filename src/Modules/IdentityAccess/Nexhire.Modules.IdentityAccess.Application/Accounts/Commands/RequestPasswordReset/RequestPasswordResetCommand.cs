using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.RequestPasswordReset;

public record RequestPasswordResetCommand(string Identifier) : ICommand;
