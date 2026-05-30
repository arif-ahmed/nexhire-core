using FluentValidation;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.VerifyPasswordResetOtp;

public class VerifyPasswordResetOtpCommandValidator : AbstractValidator<VerifyPasswordResetOtpCommand>
{
    public VerifyPasswordResetOtpCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]+$");
    }
}
