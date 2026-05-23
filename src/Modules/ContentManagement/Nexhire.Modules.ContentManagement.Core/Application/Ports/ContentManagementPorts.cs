using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Application.Ports;

public record SeekerPersonalizationAttributes(
    Guid UserId,
    string? Sector,
    string? Location,
    IReadOnlyList<string> JobInterests);

public interface IJobSeekerProfileQueryApi
{
    Task<SeekerPersonalizationAttributes?> GetPersonalizationAttributesAsync(Guid userId, CancellationToken ct);
}

public interface IMediaStorage
{
    Task<Result<MediaStorageResult>> StoreAsync(byte[] content, string fileName, string mimeType, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
    Task<Result<string>> GetPublicUrlAsync(string storageKey, CancellationToken ct);
}

public record MediaStorageResult(string StorageKey, string Url, string MimeType, long SizeBytes);
