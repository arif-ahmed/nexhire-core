using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

public enum MfaMethod
{
    None,
    Totp,
    SmsOtp
}

public class MfaConfiguration : ValueObject
{
    public bool Enabled { get; }
    public MfaMethod Method { get; }
    public string? SecretRef { get; }

    private MfaConfiguration() { } // EF Core

    private MfaConfiguration(bool enabled, MfaMethod method, string? secretRef)
    {
        Enabled = enabled;
        Method = method;
        SecretRef = secretRef;
    }

    public static MfaConfiguration CreateDisabled()
    {
        return new MfaConfiguration(false, MfaMethod.None, null);
    }

    public static Result<MfaConfiguration> CreateEnabled(MfaMethod method, string? secretRef)
    {
        if (method == MfaMethod.None)
            return Result.Failure<MfaConfiguration>(new Error("Mfa.InvalidConfiguration", "MFA method cannot be None when enabled."));

        if (string.IsNullOrWhiteSpace(secretRef))
            return Result.Failure<MfaConfiguration>(new Error("Mfa.InvalidConfiguration", "Secret reference is required when MFA is enabled."));

        return new MfaConfiguration(true, method, secretRef);
    }

    public MfaConfiguration Disable()
    {
        return CreateDisabled();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Enabled;
        yield return Method;
        yield return SecretRef ?? string.Empty;
    }
}
