using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain;

public class BackupCode : Entity<BackupCodeId>
{
    public string CodeHash { get; private set; }
    public DateTime? UsedOnUtc { get; private set; }
    public bool IsUsed => UsedOnUtc.HasValue;

    private BackupCode() { }

    internal BackupCode(BackupCodeId id, string codeHash) : base(id)
    {
        CodeHash = codeHash;
    }

    public Result Redeem(DateTime utcNow)
    {
        if (IsUsed)
            return Result.Failure(new Error("BackupCode.AlreadyUsed", "This backup code has already been used."));
            
        UsedOnUtc = utcNow;
        return Result.Success();
    }
}
