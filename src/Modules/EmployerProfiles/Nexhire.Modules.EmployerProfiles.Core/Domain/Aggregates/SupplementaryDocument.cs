using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;

public class SupplementaryDocument : Entity<Guid>
{
    public FileReference File { get; private set; } = null!;
    public DocumentKind Kind { get; private set; }
    public VirusScanResult ScanResult { get; private set; } = null!;
    public DateTime UploadedOnUtc { get; private set; }

    private SupplementaryDocument(Guid id, FileReference file, DocumentKind kind, VirusScanResult scanResult) : base(id)
    {
        File = file;
        Kind = kind;
        ScanResult = scanResult;
        UploadedOnUtc = DateTime.UtcNow;
    }

    private SupplementaryDocument()
    {
        // Required by EF Core
    }

    public static SupplementaryDocument Create(Guid id, FileReference file, DocumentKind kind, VirusScanResult scanResult)
    {
        return new SupplementaryDocument(id, file, kind, scanResult);
    }
}
