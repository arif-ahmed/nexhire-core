using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Services;

public class UploadPolicyService
{
    private static readonly HashSet<string> ImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg"
    };

    private static readonly HashSet<string> DocumentMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/jpg"
    };

    public static Result ValidateLogo(FileReference file, VirusScanResult scan)
    {
        if (scan.Status == VirusScanStatus.Infected)
        {
            return Result.Failure(new Error("E-UPLOAD-VIRUS", "The uploaded file is infected."));
        }

        if (scan.Status == VirusScanStatus.Pending)
        {
            return Result.Failure(new Error("E-UPLOAD-PENDING", "The file virus scan is pending."));
        }

        if (!ImageMimeTypes.Contains(file.MimeType))
        {
            return Result.Failure(new Error("E-UPLOAD-INVALID-FORMAT", "Unsupported logo format. Only PNG or JPG are allowed."));
        }

        if (file.SizeBytes > 5 * 1024 * 1024)
        {
            return Result.Failure(new Error("E-UPLOAD-SIZE-EXCEEDED", "Logo size must not exceed 5 MB."));
        }

        return Result.Success();
    }

    public static Result ValidateCompanyImage(FileReference file, VirusScanResult scan, int currentImageCount)
    {
        if (currentImageCount >= 5)
        {
            return Result.Failure(new Error("E-UPLOAD-LIMIT-EXCEEDED", "Company gallery cannot exceed 5 images."));
        }

        if (scan.Status == VirusScanStatus.Infected)
        {
            return Result.Failure(new Error("E-UPLOAD-VIRUS", "The uploaded image is infected."));
        }

        if (scan.Status == VirusScanStatus.Pending)
        {
            return Result.Failure(new Error("E-UPLOAD-PENDING", "The image virus scan is pending."));
        }

        if (!ImageMimeTypes.Contains(file.MimeType))
        {
            return Result.Failure(new Error("E-UPLOAD-INVALID-FORMAT", "Unsupported image format. Only PNG or JPG are allowed."));
        }

        if (file.SizeBytes > 5 * 1024 * 1024)
        {
            return Result.Failure(new Error("E-UPLOAD-SIZE-EXCEEDED", "Image size must not exceed 5 MB."));
        }

        return Result.Success();
    }

    public static Result ValidateSupplementaryDocument(FileReference file, VirusScanResult scan, int currentDocCount)
    {
        if (currentDocCount >= 10)
        {
            return Result.Failure(new Error("E-UPLOAD-LIMIT-EXCEEDED", "Supplementary documents cannot exceed 10 files."));
        }

        if (scan.Status == VirusScanStatus.Infected)
        {
            return Result.Failure(new Error("E-UPLOAD-VIRUS", "The uploaded document is infected."));
        }

        if (scan.Status == VirusScanStatus.Pending)
        {
            return Result.Failure(new Error("E-UPLOAD-PENDING", "The document virus scan is pending."));
        }

        if (!DocumentMimeTypes.Contains(file.MimeType))
        {
            return Result.Failure(new Error("E-UPLOAD-INVALID-FORMAT", "Unsupported document format. Only PDF, PNG, or JPG are allowed."));
        }

        if (file.SizeBytes > 10 * 1024 * 1024)
        {
            return Result.Failure(new Error("E-UPLOAD-SIZE-EXCEEDED", "Document size must not exceed 10 MB."));
        }

        return Result.Success();
    }
}
