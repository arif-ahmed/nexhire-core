using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.VerifyPasswordResetOtp;

public record VerifyPasswordResetOtpCommand(
    string Identifier,
    string Code) : ICommand<VerifyPasswordResetOtpResultDto>;
