using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;

public sealed class MediaReference : ValueObject
{
    private static readonly HashSet<string> ImageMimeTypes = ["image/jpeg", "image/png", "image/gif"];
    private static readonly HashSet<string> VideoMimeTypes = ["video/mp4", "video/webm"];
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;       // 5 MB
    private const long MaxVideoSizeBytes = 500 * 1024 * 1024;      // 500 MB

    public string StorageKey { get; }
    public string Url { get; }
    public string MimeType { get; }
    public long SizeBytes { get; }
    public MediaKind Kind { get; }
    public string? TranscriptUrl { get; }

    private MediaReference(string storageKey, string url, string mimeType, long sizeBytes, MediaKind kind, string? transcriptUrl)
    {
        StorageKey = storageKey;
        Url = url;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        Kind = kind;
        TranscriptUrl = transcriptUrl;
    }

    public static Result<MediaReference> Create(
        string storageKey, string url, string mimeType, long sizeBytes, MediaKind kind, string? transcriptUrl = null)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return Result.Failure<MediaReference>(new Error("E-MEDIA-KEY-EMPTY", "Storage key cannot be empty."));

        if (sizeBytes <= 0)
            return Result.Failure<MediaReference>(new Error("E-MEDIA-SIZE-INVALID", "Size must be greater than zero."));

        if (kind == MediaKind.Image)
        {
            if (!ImageMimeTypes.Contains(mimeType))
                return Result.Failure<MediaReference>(new Error("E-MEDIA-INVALID-FORMAT", $"Image MIME type must be one of: {string.Join(", ", ImageMimeTypes)}."));

            if (sizeBytes > MaxImageSizeBytes)
                return Result.Failure<MediaReference>(new Error("E-MEDIA-SIZE-EXCEEDED", $"Image size cannot exceed {MaxImageSizeBytes / (1024 * 1024)} MB."));
        }
        else
        {
            if (!VideoMimeTypes.Contains(mimeType))
                return Result.Failure<MediaReference>(new Error("E-MEDIA-INVALID-FORMAT", $"Video MIME type must be one of: {string.Join(", ", VideoMimeTypes)}."));

            if (sizeBytes > MaxVideoSizeBytes)
                return Result.Failure<MediaReference>(new Error("E-MEDIA-SIZE-EXCEEDED", $"Video size cannot exceed {MaxVideoSizeBytes / (1024 * 1024)} MB."));
        }

        return Result.Success(new MediaReference(storageKey, url, mimeType, sizeBytes, kind, transcriptUrl));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return StorageKey;
    }
}
