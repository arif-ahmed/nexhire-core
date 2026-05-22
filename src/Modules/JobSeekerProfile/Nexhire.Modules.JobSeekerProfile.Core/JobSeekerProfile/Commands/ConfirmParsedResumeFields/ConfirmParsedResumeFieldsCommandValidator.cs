using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.ConfirmParsedResumeFields;

public class ConfirmParsedResumeFieldsCommandValidator : AbstractValidator<ConfirmParsedResumeFieldsCommand>
{
    public ConfirmParsedResumeFieldsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.ResumeId)
            .NotEmpty().WithMessage("ResumeId is required.");

        RuleFor(x => x.SelectedFieldKeys)
            .NotNull().WithMessage("SelectedFieldKeys list cannot be null.");
    }
}
