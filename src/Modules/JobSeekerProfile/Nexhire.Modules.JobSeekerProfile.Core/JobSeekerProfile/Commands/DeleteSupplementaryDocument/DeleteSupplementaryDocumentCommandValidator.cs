using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.DeleteSupplementaryDocument;

public class DeleteSupplementaryDocumentCommandValidator : AbstractValidator<DeleteSupplementaryDocumentCommand>
{
    public DeleteSupplementaryDocumentCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.DocumentId)
            .NotEmpty().WithMessage("DocumentId is required.");
    }
}
