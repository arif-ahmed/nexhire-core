using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Services;

public static class TokenClaimsBuilder
{
    public static Result<AccessTokenSpec> BuildAccessToken(UserAccount account, Guid sessionId, IReadOnlyList<string> scopes, TimeSpan ttl)
    {
        var expiresOnUtc = DateTime.UtcNow.Add(ttl);
        return AccessTokenSpec.Create(
            account.Id.Value,
            account.Role.ToString(),
            account.Permissions,
            scopes,
            sessionId,
            expiresOnUtc);
    }
}
