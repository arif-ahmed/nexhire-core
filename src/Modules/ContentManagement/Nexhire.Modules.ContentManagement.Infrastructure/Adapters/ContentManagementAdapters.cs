using Nexhire.Modules.ContentManagement.Core.Application.Ports;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Infrastructure.Adapters;

public sealed class StubMediaStorage : IMediaStorage
{
    public Task<Result<MediaStorageResult>> StoreAsync(byte[] content, string fileName, string mimeType, CancellationToken ct)
    {
        var key = Guid.NewGuid().ToString("N");
        return Task.FromResult(Result.Success(new MediaStorageResult(key, $"/media/{key}", mimeType, content.Length)));
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct) => Task.CompletedTask;

    public Task<Result<string>> GetPublicUrlAsync(string storageKey, CancellationToken ct)
        => Task.FromResult(Result.Success($"/media/{storageKey}"));
}

public sealed class StubJobSeekerProfileQueryApi : IJobSeekerProfileQueryApi
{
    public Task<SeekerPersonalizationAttributes?> GetPersonalizationAttributesAsync(Guid userId, CancellationToken ct)
        => Task.FromResult<SeekerPersonalizationAttributes?>(null);
}
