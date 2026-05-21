using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;

public interface IVirusScanner
{
    Task<VirusScanResult> ScanAsync(FileReference file, CancellationToken cancellationToken = default);
}
