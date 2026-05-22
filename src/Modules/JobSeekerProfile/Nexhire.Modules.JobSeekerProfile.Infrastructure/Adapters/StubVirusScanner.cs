using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Adapters;

public class StubVirusScanner : IVirusScanner
{
    public Task<VirusScanResult> ScanAsync(
        FileReference file,
        CancellationToken cancellationToken = default)
    {
        var scanResult = VirusScanResult.Create(VirusScanStatus.Clean, DateTime.UtcNow).Value;
        return Task.FromResult(scanResult);
    }
}
