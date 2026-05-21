using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Adapters;

public class StubVirusScanner : IVirusScanner
{
    public Task<VirusScanResult> ScanAsync(FileReference file, CancellationToken cancellationToken = default)
    {
        var status = (file.OriginalFileName.Contains("infected", StringComparison.OrdinalIgnoreCase) || 
                      file.StorageKey.Contains("infected", StringComparison.OrdinalIgnoreCase))
            ? VirusScanStatus.Infected
            : VirusScanStatus.Clean;

        var scanResult = VirusScanResult.Create(status, DateTime.UtcNow).Value;
        return Task.FromResult(scanResult);
    }
}
