using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Application.Ports;

public interface IJwtSigner
{
    string SignAccessToken(AccessTokenSpec spec);
    (string Token, string TokenHash) IssueRefreshToken();
    bool ValidateSignature(string token);
}
