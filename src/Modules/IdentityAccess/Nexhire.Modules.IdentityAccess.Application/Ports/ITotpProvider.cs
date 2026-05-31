namespace Nexhire.Modules.IdentityAccess.Application.Ports;

public interface ITotpProvider
{
    (string SecretRef, string ProvisioningUri) Enroll(string accountLabel);
    bool Verify(string secretRef, string submittedCode);
}
