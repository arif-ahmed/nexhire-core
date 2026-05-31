using FluentValidation;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.CompletePasswordReset;

public class CompletePasswordResetCommandValidator : AbstractValidator<CompletePasswordResetCommand>
{
    public CompletePasswordResetCommandValidator()
    {
        RuleFor(x => x.ResetToken)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(10)
            .Must(HasThreeCharacterClasses);
    }

    private bool HasThreeCharacterClasses(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;
        
        int classes = 0;
        if (password.Any(char.IsUpper)) classes++;
        if (password.Any(char.IsLower)) classes++;
        if (password.Any(char.IsDigit)) classes++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) classes++;
        
        return classes >= 3;
    }
}
