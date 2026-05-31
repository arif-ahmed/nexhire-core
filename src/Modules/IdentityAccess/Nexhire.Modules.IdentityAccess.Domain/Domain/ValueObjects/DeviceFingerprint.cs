using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

public class DeviceFingerprint : ValueObject
{
    public string Value { get; }

    private DeviceFingerprint() { } // EF Core

    private DeviceFingerprint(string value)
    {
        Value = value!;
    }

    public static Result<DeviceFingerprint> Create(string fingerprint)
    {
        var trimmed = fingerprint?.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
            return Result.Failure<DeviceFingerprint>(new Error("DeviceFingerprint.Empty", "Device fingerprint cannot be empty."));

        return new DeviceFingerprint(trimmed);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
