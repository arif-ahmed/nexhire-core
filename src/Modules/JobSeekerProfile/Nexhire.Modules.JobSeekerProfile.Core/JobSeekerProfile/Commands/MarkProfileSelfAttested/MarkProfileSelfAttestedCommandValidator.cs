using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.MarkProfileSelfAttested;

public class MarkProfileSelfAttestedCommandValidator : AbstractValidator<MarkProfileSelfAttestedCommand>
{
    public MarkProfileSelfAttestedCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
