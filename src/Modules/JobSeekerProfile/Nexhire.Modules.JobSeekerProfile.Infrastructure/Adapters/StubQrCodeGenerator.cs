using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Adapters;

public class StubQrCodeGenerator : IQrCodeGenerator
{
    public Task<Result<FileReference>> GenerateAsync(
        string publicUrl,
        CancellationToken cancellationToken = default)
    {
        var key = $"qr/{Guid.NewGuid()}_qrcode.png";
        var fileRef = FileReference.Create(key, "qrcode.png", "image/png", 2048).Value;
        return Task.FromResult(Result.Success(fileRef));
    }
}
