using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ResendActivationOtp;

public record ResendActivationOtpCommand(Guid UserId) : ICommand;
