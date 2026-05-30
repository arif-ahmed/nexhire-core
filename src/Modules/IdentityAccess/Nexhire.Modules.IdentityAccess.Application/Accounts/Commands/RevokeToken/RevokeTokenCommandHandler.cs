using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using System.Security.Cryptography;
using System.Text;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.RevokeToken;

public class RevokeTokenCommandHandler : ICommandHandler<RevokeTokenCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRevokedTokenStore _revokedTokenStore;

    public RevokeTokenCommandHandler(
        IUserAccountRepository userAccountRepository,
        IRevokedTokenStore revokedTokenStore)
    {
        _userAccountRepository = userAccountRepository;
        _revokedTokenStore = revokedTokenStore;
    }

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        
        await _revokedTokenStore.AddAsync(tokenHash, DateTime.UtcNow.AddDays(30), cancellationToken);

        var account = await _userAccountRepository.GetBySessionRefreshTokenHashAsync(tokenHash, cancellationToken);
        if (account != null)
        {
            var session = account.Sessions.FirstOrDefault(s => s.RefreshTokenHash == tokenHash);
            if (session != null)
            {
                account.RevokeSession(session.Id);
            }
        }

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

