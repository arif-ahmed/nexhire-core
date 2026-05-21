using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public class FileReference : ValueObject
{
    public string StorageKey { get; }
    public string OriginalFileName { get; }
    public string MimeType { get; }
    public long SizeBytes { get; }

    private FileReference(string storageKey, string originalFileName, string mimeType, long sizeBytes)
    {
        StorageKey = storageKey;
        OriginalFileName = originalFileName;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
    }

    public static Result<FileReference> Create(string storageKey, string originalFileName, string mimeType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || 
            string.IsNullOrWhiteSpace(originalFileName) || 
            string.IsNullOrWhiteSpace(mimeType))
        {
            return Result.Failure<FileReference>(new Error("FileReference.InvalidInput", "Storage key, file name, and MIME type are required."));
        }

        if (sizeBytes <= 0)
        {
            return Result.Failure<FileReference>(new Error("FileReference.InvalidSize", "File size must be greater than zero."));
        }

        return Result.Success(new FileReference(storageKey.Trim(), originalFileName.Trim(), mimeType.Trim(), sizeBytes));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return StorageKey;
        yield return OriginalFileName;
        yield return MimeType;
        yield return SizeBytes;
    }
}
