using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UploadSupplementaryDocument;

public class UploadSupplementaryDocumentCommandValidator : AbstractValidator<UploadSupplementaryDocumentCommand>
{
    public UploadSupplementaryDocumentCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.");

        RuleFor(x => x.MimeType)
            .NotEmpty().WithMessage("MimeType is required.");

        RuleFor(x => x.Kind)
            .NotEmpty().WithMessage("Kind is required.");
    }
}
