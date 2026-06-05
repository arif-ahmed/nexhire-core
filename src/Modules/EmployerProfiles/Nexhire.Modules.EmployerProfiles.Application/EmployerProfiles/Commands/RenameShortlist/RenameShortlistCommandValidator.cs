using FluentValidation;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.RenameShortlist;

public class RenameShortlistCommandValidator : AbstractValidator<RenameShortlistCommand>
{
    public RenameShortlistCommandValidator()
    {
        RuleFor(x => x.NewName)
            .NotEmpty()
            .WithMessage("Shortlist name cannot be empty.")
            .MaximumLength(100)
            .WithMessage("Shortlist name must not exceed 100 characters.");
    }
}
