using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddExperienceEntry;

public class AddExperienceEntryCommandValidator : AbstractValidator<AddExperienceEntryCommand>
{
    public AddExperienceEntryCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Company)
            .NotEmpty().WithMessage("Company is required.")
            .MaximumLength(150).WithMessage("Company must not exceed 150 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(150).WithMessage("Role must not exceed 150 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .Null().When(x => x.IsCurrent)
            .WithMessage("End date must be empty for current experience entry.")
            .WithErrorCode("ExperienceEntry.InvalidCurrentEndDate");

        RuleFor(x => x.EndDate)
            .Must((cmd, endDate) => !endDate.HasValue || cmd.StartDate <= endDate.Value)
            .When(x => !x.IsCurrent)
            .WithMessage("Start date must be before or equal to end date.");
    }
}
