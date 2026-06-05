using FluentValidation;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.UploadEmployerDocument;

public class UploadEmployerDocumentCommandValidator : AbstractValidator<UploadEmployerDocumentCommand>
{
    private static readonly string[] AllowedMimeTypes = ["application/pdf", "image/png", "image/jpeg"];
    private const int MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadEmployerDocumentCommandValidator()
    {
        RuleFor(x => x.MimeType)
            .Must(m => AllowedMimeTypes.Contains(m))
            .WithMessage("Document must be PDF, PNG, or JPG format.")
            .WithErrorCode("E-UPLOAD-INVALID-FORMAT");

        RuleFor(x => x.Content)
            .NotNull()
            .Must(content => content == null || content.Length <= MaxSizeBytes)
            .WithMessage("Document size must not exceed 10 MB.")
            .WithErrorCode("E-UPLOAD-SIZE-EXCEEDED");
    }
}
