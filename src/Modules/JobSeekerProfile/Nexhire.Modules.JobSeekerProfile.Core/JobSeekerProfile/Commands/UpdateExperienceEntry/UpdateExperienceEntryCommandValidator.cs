using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UpdateExperienceEntry;

public class UpdateExperienceEntryCommandValidator : AbstractValidator<UpdateExperienceEntryCommand>
{
    public UpdateExperienceEntryCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.ExperienceEntryId)
            .NotEmpty().WithMessage("ExperienceEntryId is required.");

        RuleFor(x => x.Company)
            .NotEmpty().WithMessage("Company is required.")
            .MaximumLength(150).WithMessage("Company must not exceed 150 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(150).WithMessage("Role must not exceed 150 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .Must((cmd, endDate) => !endDate.HasValue || cmd.StartDate <= endDate.Value)
            .WithMessage("Start date must be before or equal to end date.");
    }
}
