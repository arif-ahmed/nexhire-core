namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;

public interface IOtpChallengeRepository
{
    Task<OtpChallenge?> GetByIdAsync(OtpChallengeId id, CancellationToken ct = default);
    Task<OtpChallenge?> GetActiveByAccountAndPurposeAsync(UserAccountId accountId, OtpPurpose purpose, CancellationToken ct = default);
    Task AddAsync(OtpChallenge challenge, CancellationToken ct = default);
}
