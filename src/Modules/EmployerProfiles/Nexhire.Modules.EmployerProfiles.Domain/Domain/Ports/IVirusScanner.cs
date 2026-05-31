using Nexhire.Modules.EmployerProfiles.Domain.ValueObjects;

namespace Nexhire.Modules.EmployerProfiles.Domain.Ports;

public interface IVirusScanner
{
    Task<VirusScanResult> ScanAsync(FileReference file, CancellationToken cancellationToken = default);
}
