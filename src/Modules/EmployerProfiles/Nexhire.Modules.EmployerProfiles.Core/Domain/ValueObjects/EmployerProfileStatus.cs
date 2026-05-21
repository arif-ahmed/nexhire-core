namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public enum EmployerProfileStatus
{
    PendingActivation,
    PendingVerification,
    PendingManualVerification,
    Verified,
    Rejected,
    Suspended,
    Deactivated
}
