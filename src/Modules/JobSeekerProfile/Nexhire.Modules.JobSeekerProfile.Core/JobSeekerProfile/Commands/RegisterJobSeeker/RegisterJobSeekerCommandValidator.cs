using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RegisterJobSeeker;

public class RegisterJobSeekerCommandValidator : AbstractValidator<RegisterJobSeekerCommand>
{
    public RegisterJobSeekerCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .WithErrorCode("E-REG-INVALID-EMAIL");

        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(@"^\+[1-9]\d{1,14}$").WithMessage("Mobile number must be in valid E.164 format.")
            .WithErrorCode("E-REG-INVALID-MOBILE");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .WithErrorCode("E-REG-INVALID-PASSWORD");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .Must(g => Enum.TryParse<Domain.ValueObjects.Gender>(g, true, out _))
            .WithMessage("Invalid gender value.");
    }
}
