using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.CompletePasswordReset;

public record CompletePasswordResetCommand(
    string Identifier,
    string ResetToken,
    string NewPassword) : ICommand;
