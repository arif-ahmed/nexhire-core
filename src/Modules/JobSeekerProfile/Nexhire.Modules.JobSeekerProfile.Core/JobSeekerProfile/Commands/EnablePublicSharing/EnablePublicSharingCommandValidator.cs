using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.EnablePublicSharing;

public class EnablePublicSharingCommandValidator : AbstractValidator<EnablePublicSharingCommand>
{
    public EnablePublicSharingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
