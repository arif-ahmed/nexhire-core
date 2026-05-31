using FluentValidation;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.EnrollMfa;

public class EnrollMfaCommandValidator : AbstractValidator<EnrollMfaCommand>
{
    public EnrollMfaCommandValidator()
    {
        RuleFor(x => x.Method)
            .Must(m => m.Equals("Totp", StringComparison.OrdinalIgnoreCase) || m.Equals("SmsOtp", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Method must be Totp or SmsOtp");
    }
}
