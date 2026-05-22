using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;

public interface IQrCodeGenerator
{
    Task<Result<FileReference>> GenerateAsync(string publicUrl, CancellationToken cancellationToken = default);
}
