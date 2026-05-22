using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.DisablePublicSharing;

public class DisablePublicSharingCommandValidator : AbstractValidator<DisablePublicSharingCommand>
{
    public DisablePublicSharingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
