using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetProfileVisibility;

public class SetProfileVisibilityCommandValidator : AbstractValidator<SetProfileVisibilityCommand>
{
    public SetProfileVisibilityCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Visibility)
            .NotEmpty().WithMessage("Visibility is required.");
    }
}
