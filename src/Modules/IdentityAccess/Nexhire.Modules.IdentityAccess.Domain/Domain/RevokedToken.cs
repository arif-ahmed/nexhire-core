namespace Nexhire.Modules.IdentityAccess.Domain.Domain;

public sealed class RevokedToken
{
    public string TokenIdOrRefreshHash { get; private set; }
    public DateTime RevokedOnUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }

    private RevokedToken() { } // EF Core

    public RevokedToken(string tokenIdOrRefreshHash, DateTime revokedOnUtc, DateTime expiresOnUtc)
    {
        TokenIdOrRefreshHash = tokenIdOrRefreshHash;
        RevokedOnUtc = revokedOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }
}
