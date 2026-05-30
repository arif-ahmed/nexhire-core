using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

public class LockState : ValueObject
{
    public bool IsLocked { get; }
    public DateTime? LockedUntilUtc { get; }
    public int FailedLoginCount { get; }
    public int FailedOtpCount { get; }

    private LockState(bool isLocked, DateTime? lockedUntilUtc, int failedLoginCount, int failedOtpCount)
    {
        IsLocked = isLocked;
        LockedUntilUtc = lockedUntilUtc;
        FailedLoginCount = Math.Max(0, failedLoginCount);
        FailedOtpCount = Math.Max(0, failedOtpCount);
    }

    public static LockState CreateUnlocked()
    {
        return new LockState(false, null, 0, 0);
    }

    public static LockState CreateLocked(DateTime lockedUntilUtc, int failedLoginCount, int failedOtpCount)
    {
        return new LockState(true, lockedUntilUtc, failedLoginCount, failedOtpCount);
    }

    public LockState IncrementFailedLogin()
    {
        return new LockState(IsLocked, LockedUntilUtc, FailedLoginCount + 1, FailedOtpCount);
    }

    public LockState IncrementFailedOtp()
    {
        return new LockState(IsLocked, LockedUntilUtc, FailedLoginCount, FailedOtpCount + 1);
    }

    public LockState Lock(TimeSpan duration)
    {
        return new LockState(true, DateTime.UtcNow.Add(duration), FailedLoginCount, FailedOtpCount);
    }

    public LockState Unlock()
    {
        return CreateUnlocked();
    }

    public bool IsExpired()
    {
        if (!IsLocked || LockedUntilUtc == null)
            return false;

        return DateTime.UtcNow >= LockedUntilUtc;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsLocked;
        yield return LockedUntilUtc;
        yield return FailedLoginCount;
        yield return FailedOtpCount;
    }
}
