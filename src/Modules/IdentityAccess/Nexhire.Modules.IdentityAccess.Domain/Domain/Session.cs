using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain;

public class Session : Entity<SessionId>
{
    public UserAccountId UserAccountId { get; private set; }
    public Channel Channel { get; private set; }
    public DeviceFingerprint DeviceFingerprint { get; private set; }
    public string RefreshTokenHash { get; private set; }
    public DateTime IssuedOnUtc { get; private set; }
    public DateTime LastSeenUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }

    public bool IsRevoked => RevokedOnUtc.HasValue;

    private Session() { } // EF Core

    internal Session(
        SessionId id,
        UserAccountId userAccountId,
        Channel channel,
        DeviceFingerprint deviceFingerprint,
        string refreshTokenHash,
        DateTime issuedOnUtc,
        DateTime expiresOnUtc) : base(id)
    {
        UserAccountId = userAccountId;
        Channel = channel;
        DeviceFingerprint = deviceFingerprint;
        RefreshTokenHash = refreshTokenHash;
        IssuedOnUtc = issuedOnUtc;
        LastSeenUtc = issuedOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresOnUtc;
    
    public bool IsInactive(DateTime utcNow, TimeSpan inactivityTimeout)
        => utcNow - LastSeenUtc > inactivityTimeout;

    public void Revoke(DateTime utcNow)
    {
        if (!IsRevoked)
            RevokedOnUtc = utcNow;
    }

    public void Touch(DateTime utcNow)
    {
        LastSeenUtc = utcNow;
    }
}
