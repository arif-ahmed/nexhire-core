using FluentValidation;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.UploadCompanyImage;

public class UploadCompanyImageCommandValidator : AbstractValidator<UploadCompanyImageCommand>
{
    private static readonly string[] AllowedMimeTypes = ["image/png", "image/jpeg"];
    private const int MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

    public UploadCompanyImageCommandValidator()
    {
        RuleFor(x => x.MimeType)
            .Must(m => AllowedMimeTypes.Contains(m))
            .WithMessage("Company image must be PNG or JPG format.")
            .WithErrorCode("E-UPLOAD-INVALID-FORMAT");

        RuleFor(x => x.Content)
            .NotNull()
            .Must(content => content == null || content.Length <= MaxSizeBytes)
            .WithMessage("Company image size must not exceed 5 MB.")
            .WithErrorCode("E-UPLOAD-SIZE-EXCEEDED");
    }
}
