using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RegeneratePublicSlug;

public class RegeneratePublicSlugCommandValidator : AbstractValidator<RegeneratePublicSlugCommand>
{
    public RegeneratePublicSlugCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
