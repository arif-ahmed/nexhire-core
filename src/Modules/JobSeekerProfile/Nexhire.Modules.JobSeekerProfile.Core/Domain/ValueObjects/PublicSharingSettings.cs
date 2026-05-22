using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class PublicSharingSettings : ValueObject
{
    public bool Enabled { get; }
    public string? Slug { get; }
    public FileReference? QrCodeRef { get; }

    private PublicSharingSettings(bool enabled, string? slug, FileReference? qrCodeRef)
    {
        Enabled = enabled;
        Slug = slug;
        QrCodeRef = qrCodeRef;
    }

    public static Result<PublicSharingSettings> CreateDisabled()
    {
        return Result.Success(new PublicSharingSettings(false, null, null));
    }

    public static Result<PublicSharingSettings> CreateEnabled(string slug, FileReference qrCodeRef)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result.Failure<PublicSharingSettings>(new Error("PublicSharingSettings.EmptySlug", "Slug is required when public sharing is enabled."));
        }

        if (qrCodeRef == null)
        {
            return Result.Failure<PublicSharingSettings>(new Error("PublicSharingSettings.NullQrCode", "QR code reference is required when public sharing is enabled."));
        }

        return Result.Success(new PublicSharingSettings(true, slug.Trim().ToLowerInvariant(), qrCodeRef));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Enabled;
        if (Slug != null) yield return Slug;
        if (QrCodeRef != null) yield return QrCodeRef;
    }
}
