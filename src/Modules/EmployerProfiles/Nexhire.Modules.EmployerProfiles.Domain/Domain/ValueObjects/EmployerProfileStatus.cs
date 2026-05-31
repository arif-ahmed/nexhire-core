namespace Nexhire.Modules.EmployerProfiles.Domain.ValueObjects;

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
