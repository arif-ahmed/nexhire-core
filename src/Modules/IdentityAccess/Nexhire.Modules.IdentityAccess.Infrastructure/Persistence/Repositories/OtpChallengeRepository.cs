using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence.Repositories;

public class OtpChallengeRepository : IOtpChallengeRepository
{
    private readonly IdentityAccessDbContext _dbContext;

    public OtpChallengeRepository(IdentityAccessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OtpChallenge?> GetByIdAsync(OtpChallengeId id, CancellationToken ct = default)
    {
        return await _dbContext.OtpChallenges.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<OtpChallenge?> GetActiveByAccountAndPurposeAsync(UserAccountId accountId, OtpPurpose purpose, CancellationToken ct = default)
    {
        return await _dbContext.OtpChallenges
            .Where(x => x.UserAccountId == accountId && x.Purpose == purpose && x.Status == OtpStatus.Issued && x.ExpiresOnUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.IssuedOnUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(OtpChallenge challenge, CancellationToken ct = default)
    {
        await _dbContext.OtpChallenges.AddAsync(challenge, ct);
    }
}
