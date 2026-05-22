using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

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

    public static Result<SupplementaryDocument> Create(Guid id, FileReference file, DocumentKind kind, VirusScanResult scanResult)
    {
        if (file == null)
        {
            return Result.Failure<SupplementaryDocument>(new Error("SupplementaryDocument.NullFile", "File reference is required."));
        }

        if (scanResult == null)
        {
            return Result.Failure<SupplementaryDocument>(new Error("SupplementaryDocument.NullScanResult", "Virus scan result is required."));
        }

        if (scanResult.Status != VirusScanStatus.Clean)
        {
            return Result.Failure<SupplementaryDocument>(new Error("E-UPLOAD-VIRUS", "Only clean files can be uploaded as supplementary documents."));
        }

        return Result.Success(new SupplementaryDocument(id, file, kind, scanResult));
    }
}
