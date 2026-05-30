using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain;

public class TrustedDevice : Entity<TrustedDeviceId>
{
    public DeviceFingerprint DeviceFingerprint { get; private set; }
    public string Label { get; private set; }
    public DateTime TrustedUntilUtc { get; private set; }

    private TrustedDevice() 
    { 
        DeviceFingerprint = null!;
        Label = null!;
    }

    internal TrustedDevice(TrustedDeviceId id, DeviceFingerprint deviceFingerprint, string label, DateTime trustedUntilUtc) : base(id)
    {
        DeviceFingerprint = deviceFingerprint;
        Label = label;
        TrustedUntilUtc = trustedUntilUtc;
    }
}
