using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveEducationEntry;

public class RemoveEducationEntryCommandValidator : AbstractValidator<RemoveEducationEntryCommand>
{
    public RemoveEducationEntryCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.EducationEntryId)
            .NotEmpty().WithMessage("EducationEntryId is required.");
    }
}
