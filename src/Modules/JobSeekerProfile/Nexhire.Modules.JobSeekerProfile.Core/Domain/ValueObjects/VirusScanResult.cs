using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public enum VirusScanStatus
{
    Pending,
    Clean,
    Infected
}

public class VirusScanResult : ValueObject
{
    public VirusScanStatus Status { get; }
    public DateTime? ScannedOnUtc { get; }

    private VirusScanResult(VirusScanStatus status, DateTime? scannedOnUtc)
    {
        Status = status;
        ScannedOnUtc = scannedOnUtc;
    }

    public static Result<VirusScanResult> Create(VirusScanStatus status, DateTime? scannedOnUtc = null)
    {
        return Result.Success(new VirusScanResult(status, scannedOnUtc));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Status;
        if (ScannedOnUtc.HasValue) yield return ScannedOnUtc.Value;
    }
}
