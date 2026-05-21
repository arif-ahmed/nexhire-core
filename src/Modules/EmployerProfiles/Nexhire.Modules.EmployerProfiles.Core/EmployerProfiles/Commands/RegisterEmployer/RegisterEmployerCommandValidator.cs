using FluentValidation;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RegisterEmployer;

public class RegisterEmployerCommandValidator : AbstractValidator<RegisterEmployerCommand>
{
    public RegisterEmployerCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .WithErrorCode("E-REG-INVALID-EMAIL");

        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(@"^\+[1-9]\d{1,14}$").WithMessage("Mobile number must be in valid E.164 format.")
            .WithErrorCode("E-REG-INVALID-MOBILE");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.");

        RuleFor(x => x.CompanyIdentifier)
            .NotEmpty().WithMessage("Company identifier is required.")
            .Matches(@"^[a-zA-Z0-9]+$").WithMessage("Company identifier must be alphanumeric.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .WithErrorCode("E-REG-INVALID-PASSWORD");
    }
}
