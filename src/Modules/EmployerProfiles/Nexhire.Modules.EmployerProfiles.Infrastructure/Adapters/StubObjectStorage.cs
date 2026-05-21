using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;
using System.Collections.Concurrent;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Adapters;

public class StubObjectStorage : IObjectStorage
{
    private static readonly ConcurrentDictionary<string, byte[]> _storage = new();

    public Task<Result<FileReference>> StoreAsync(byte[] content, string fileName, string mimeType, CancellationToken cancellationToken = default)
    {
        var storageKey = $"uploads/{Guid.NewGuid()}_{fileName}";
        _storage[storageKey] = content;

        var fileRefResult = FileReference.Create(storageKey, fileName, mimeType, content.Length);
        return Task.FromResult(fileRefResult);
    }

    public Task<Result<byte[]>> RetrieveAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (_storage.TryGetValue(storageKey, out var content))
        {
            return Task.FromResult(Result.Success(content));
        }

        return Task.FromResult(Result.Failure<byte[]>(new Error("Storage.FileNotFound", "File not found in stub storage.")));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        _storage.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }
}
