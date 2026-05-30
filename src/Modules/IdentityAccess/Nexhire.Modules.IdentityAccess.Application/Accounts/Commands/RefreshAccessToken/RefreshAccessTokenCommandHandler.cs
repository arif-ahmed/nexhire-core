using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using System.Security.Cryptography;
using System.Text;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.RefreshAccessToken;

public class RefreshAccessTokenCommandHandler : ICommandHandler<RefreshAccessTokenCommand, LoginResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IJwtSigner _jwtSigner;
    private readonly IRevokedTokenStore _revokedTokenStore;

    public RefreshAccessTokenCommandHandler(
        IUserAccountRepository userAccountRepository,
        IJwtSigner jwtSigner,
        IRevokedTokenStore revokedTokenStore)
    {
        _userAccountRepository = userAccountRepository;
        _jwtSigner = jwtSigner;
        _revokedTokenStore = revokedTokenStore;
    }

    public async Task<Result<LoginResultDto>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));
        
        if (await _revokedTokenStore.IsRevokedAsync(tokenHash, cancellationToken))
            return Result.Failure<LoginResultDto>(new Error("E-TOKEN-REVOKED", "Refresh token is revoked."));

        var account = await _userAccountRepository.GetBySessionRefreshTokenHashAsync(tokenHash, cancellationToken);
        if (account == null)
            return Result.Failure<LoginResultDto>(new Error("E-TOKEN-INVALID", "Invalid refresh token."));

        var session = account.Sessions.FirstOrDefault(s => s.RefreshTokenHash == tokenHash);
        if (session == null || session.IsRevoked || session.IsExpired(DateTime.UtcNow))
            return Result.Failure<LoginResultDto>(new Error("E-TOKEN-INVALID", "Invalid or expired session."));

        var (newRefreshToken, newRefreshTokenHash) = _jwtSigner.IssueRefreshToken();
        
        account.RevokeSession(session.Id);
        
        var newSessionId = Guid.NewGuid();
        var accessTokenSpecResult = TokenClaimsBuilder.BuildAccessToken(account, newSessionId, Array.Empty<string>(), TimeSpan.FromHours(1));
        if (accessTokenSpecResult.IsFailure) return Result.Failure<LoginResultDto>(accessTokenSpecResult.Error);

        var accessToken = _jwtSigner.SignAccessToken(accessTokenSpecResult.Value);
        
        var loginResult = account.RecordSuccessfulLogin(session.Channel, session.DeviceFingerprint, newRefreshTokenHash, DateTime.UtcNow.AddDays(30));
        if (loginResult.IsFailure) return Result.Failure<LoginResultDto>(loginResult.Error);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(new LoginResultDto(accessToken, newRefreshToken, accessTokenSpecResult.Value.ExpiresOnUtc, false, null));
    }
}

