using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Adapters;

public class StubObjectStorage : IObjectStorage
{
    public Task<Result<FileReference>> StoreAsync(
        byte[] content,
        string fileName,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        var key = $"stubs/{Guid.NewGuid()}_{fileName}";
        var fileRef = FileReference.Create(key, fileName, mimeType, content.Length).Value;
        return Task.FromResult(Result.Success(fileRef));
    }

    public Task<Result<byte[]>> RetrieveAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success(new byte[] { 1, 2, 3 }));
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
