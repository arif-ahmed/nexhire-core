using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain;

public class PasswordResetToken : Entity<PasswordResetTokenId>
{
    public string TokenHash { get; private set; }
    public DateTime IssuedOnUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime? UsedOnUtc { get; private set; }

    private PasswordResetToken() { }

    internal PasswordResetToken(PasswordResetTokenId id, string tokenHash, DateTime issuedOnUtc, DateTime expiresOnUtc) : base(id)
    {
        TokenHash = tokenHash;
        IssuedOnUtc = issuedOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresOnUtc;
    public bool IsUsed => UsedOnUtc.HasValue;

    public Result MarkUsed(DateTime utcNow)
    {
        if (IsUsed)
            return Result.Failure(new Error("PasswordResetToken.AlreadyUsed", "Token has already been used."));
            
        UsedOnUtc = utcNow;
        return Result.Success();
    }
}
