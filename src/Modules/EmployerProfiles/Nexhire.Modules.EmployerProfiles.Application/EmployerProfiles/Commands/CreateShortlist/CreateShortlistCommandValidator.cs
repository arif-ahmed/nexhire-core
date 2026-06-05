using FluentValidation;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.CreateShortlist;

public class CreateShortlistCommandValidator : AbstractValidator<CreateShortlistCommand>
{
    public CreateShortlistCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Shortlist name cannot be empty.")
            .MaximumLength(100)
            .WithMessage("Shortlist name must not exceed 100 characters.");
    }
}
