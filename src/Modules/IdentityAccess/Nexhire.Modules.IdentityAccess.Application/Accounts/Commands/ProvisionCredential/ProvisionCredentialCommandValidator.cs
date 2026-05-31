using FluentValidation;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using System.Text.RegularExpressions;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ProvisionCredential;

public class ProvisionCredentialCommandValidator : AbstractValidator<ProvisionCredentialCommand>
{
    public ProvisionCredentialCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-REG-INVALID-EMAIL")
            .EmailAddress().WithMessage("E-REG-INVALID-EMAIL");

        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("E-REG-INVALID-MOBILE")
            .Matches(@"^\+[1-9]\d{1,14}$").WithMessage("E-REG-INVALID-MOBILE");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("E-REG-INVALID-PASSWORD")
            .MinimumLength(10).WithMessage("E-REG-INVALID-PASSWORD")
            .Must(HasThreeCharacterClasses).WithMessage("E-REG-INVALID-PASSWORD");

        RuleFor(x => x.Role)
            .IsEnumName(typeof(UserRole), caseSensitive: false).WithMessage("E-REG-INVALID-ROLE");
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
