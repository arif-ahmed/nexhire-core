using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain;

public class OtpChallenge : AggregateRoot<OtpChallengeId>
{
    public UserAccountId UserAccountId { get; private set; }
    public OtpPurpose Purpose { get; private set; }
    public string CodeHash { get; private set; }
    public OtpStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }  // 5 for Activation, 3 otherwise
    public DateTime IssuedOnUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }  // IssuedOnUtc + 5min
    public DateTime? VerifiedOnUtc { get; private set; }

    private OtpChallenge() 
    { 
        UserAccountId = null!;
        CodeHash = null!;
    }

    private OtpChallenge(
        OtpChallengeId id,
        UserAccountId userAccountId,
        OtpPurpose purpose,
        string codeHash,
        int maxAttempts,
        DateTime issuedOnUtc,
        DateTime expiresOnUtc) : base(id)
    {
        UserAccountId = userAccountId;
        Purpose = purpose;
        CodeHash = codeHash;
        MaxAttempts = maxAttempts;
        IssuedOnUtc = issuedOnUtc;
        ExpiresOnUtc = expiresOnUtc;
        Status = OtpStatus.Issued;
    }

    public static OtpChallenge Issue(UserAccountId userAccountId, OtpPurpose purpose, string codeHash, TimeSpan ttl, int maxAttempts)
    {
        var now = DateTime.UtcNow;
        return new OtpChallenge(
            new OtpChallengeId(Guid.NewGuid()),
            userAccountId,
            purpose,
            codeHash,
            maxAttempts,
            now,
            now.Add(ttl)
        );
    }

    public Result<string> Verify(string submittedCodeHash, DateTime utcNow)
    {
        if (Status == OtpStatus.Expired || utcNow > ExpiresOnUtc)
        {
            MarkExpired();
            return Result.Failure<string>(new Error("E-OTP-EXPIRED", "OTP has expired."));
        }

        if (Status == OtpStatus.Locked)
        {
            return Result.Failure<string>(new Error("E-OTP-LOCKED", "Too many failed attempts."));
        }
        
        if (Status == OtpStatus.Verified)
        {
            return Result.Failure<string>(new Error("E-OTP-ALREADY-VERIFIED", "OTP has already been verified."));
        }

        AttemptCount++;

        if (submittedCodeHash != CodeHash)
        {
            if (AttemptCount >= MaxAttempts)
            {
                Status = OtpStatus.Locked;
                return Result.Failure<string>(new Error("E-OTP-LOCKED", "Too many failed attempts."));
            }
            return Result.Failure<string>(new Error("E-OTP-INVALID", "Invalid OTP."));
        }

        Status = OtpStatus.Verified;
        VerifiedOnUtc = utcNow;
        return Result.Success(submittedCodeHash);
    }

    public void MarkExpired()
    {
        if (Status == OtpStatus.Issued)
        {
            Status = OtpStatus.Expired;
        }
    }
}
