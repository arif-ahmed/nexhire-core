using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveExperienceEntry;

public class RemoveExperienceEntryCommandValidator : AbstractValidator<RemoveExperienceEntryCommand>
{
    public RemoveExperienceEntryCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.ExperienceEntryId)
            .NotEmpty().WithMessage("ExperienceEntryId is required.");
    }
}
