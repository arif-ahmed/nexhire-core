using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddEducationEntry;

public class AddEducationEntryCommandValidator : AbstractValidator<AddEducationEntryCommand>
{
    public AddEducationEntryCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Degree)
            .NotEmpty().WithMessage("Degree is required.")
            .MaximumLength(150).WithMessage("Degree must not exceed 150 characters.");

        RuleFor(x => x.Institution)
            .NotEmpty().WithMessage("Institution is required.")
            .MaximumLength(150).WithMessage("Institution must not exceed 150 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .Must((cmd, endDate) => !endDate.HasValue || cmd.StartDate <= endDate.Value)
            .WithMessage("Start date must be before or equal to end date.");
    }
}
