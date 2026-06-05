using FluentValidation;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.UploadEmployerLogo;

public class UploadEmployerLogoCommandValidator : AbstractValidator<UploadEmployerLogoCommand>
{
    private static readonly string[] AllowedMimeTypes = ["image/png", "image/jpeg"];
    private const int MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

    public UploadEmployerLogoCommandValidator()
    {
        RuleFor(x => x.MimeType)
            .Must(m => AllowedMimeTypes.Contains(m))
            .WithMessage("Logo must be PNG or JPG format.")
            .WithErrorCode("E-UPLOAD-INVALID-FORMAT");

        RuleFor(x => x.Content)
            .NotNull()
            .Must(content => content == null || content.Length <= MaxSizeBytes)
            .WithMessage("Logo size must not exceed 5 MB.")
            .WithErrorCode("E-UPLOAD-SIZE-EXCEEDED");
    }
}
