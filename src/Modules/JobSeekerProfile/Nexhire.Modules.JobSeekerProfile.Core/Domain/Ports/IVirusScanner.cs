using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;

public interface IVirusScanner
{
    Task<VirusScanResult> ScanAsync(FileReference file, CancellationToken cancellationToken = default);
}
