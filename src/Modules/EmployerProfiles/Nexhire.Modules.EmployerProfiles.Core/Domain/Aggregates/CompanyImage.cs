using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;

public class CompanyImage : Entity<Guid>
{
    public FileReference File { get; private set; } = null!;
    public VirusScanResult ScanResult { get; private set; } = null!;
    public DateTime UploadedOnUtc { get; private set; }

    private CompanyImage(Guid id, FileReference file, VirusScanResult scanResult) : base(id)
    {
        File = file;
        ScanResult = scanResult;
        UploadedOnUtc = DateTime.UtcNow;
    }

    private CompanyImage()
    {
        // Required by EF Core
    }

    public static CompanyImage Create(Guid id, FileReference file, VirusScanResult scanResult)
    {
        return new CompanyImage(id, file, scanResult);
    }
}
