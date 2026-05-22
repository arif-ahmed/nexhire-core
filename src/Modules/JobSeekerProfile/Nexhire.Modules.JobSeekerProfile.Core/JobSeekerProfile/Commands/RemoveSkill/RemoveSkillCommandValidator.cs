using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveSkill;

public class RemoveSkillCommandValidator : AbstractValidator<RemoveSkillCommand>
{
    public RemoveSkillCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.SkillId)
            .NotEmpty().WithMessage("SkillId is required.");
    }
}
