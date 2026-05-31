using OtpNet;
using Nexhire.Modules.IdentityAccess.Application.Ports;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.PortAdapters;

public class TotpProvider : ITotpProvider
{
    public (string SecretRef, string ProvisioningUri) Enroll(string accountLabel)
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(key);
        return (secret, GenerateUri(secret, accountLabel, "Nexhire"));
    }

    public string GenerateUri(string secret, string accountName, string issuer)
    {
        return $"otpauth://totp/{issuer}:{accountName}?secret={secret}&issuer={issuer}";
    }

    public bool Verify(string secret, string code)
    {
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes, step: 30, totpSize: 6);
        return totp.VerifyTotp(code, out long timeStepMatched, window: new VerificationWindow(1, 1));
    }
}
