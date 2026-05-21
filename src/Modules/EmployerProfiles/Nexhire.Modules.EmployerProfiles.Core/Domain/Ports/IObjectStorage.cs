using Nexhire.Shared.Core.Results;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;

public interface IObjectStorage
{
    Task<Result<FileReference>> StoreAsync(byte[] content, string fileName, string mimeType, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> RetrieveAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
