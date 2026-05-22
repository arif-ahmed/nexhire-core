using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddSkill;

public class AddSkillCommandValidator : AbstractValidator<AddSkillCommand>
{
    public AddSkillCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.RawLabel)
            .NotEmpty().WithMessage("Raw label is required.")
            .MaximumLength(100).WithMessage("Raw label must not exceed 100 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .Must(c => Enum.TryParse<Domain.ValueObjects.SkillCategory>(c, true, out _))
            .WithMessage("Invalid skill category.");

        RuleFor(x => x.Tier)
            .NotEmpty().WithMessage("Tier is required.")
            .Must(t => Enum.TryParse<Domain.ValueObjects.SkillTier>(t, true, out _))
            .WithMessage("Invalid skill tier.");

        RuleFor(x => x.Proficiency)
            .InclusiveBetween(1, 5).WithMessage("Proficiency must be between 1 and 5.");
    }
}
