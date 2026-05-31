using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using System.Security.Cryptography;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.VerifyMfaChallenge;

public class VerifyMfaChallengeCommandHandler : ICommandHandler<VerifyMfaChallengeCommand, LoginResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IOtpChallengeRepository _otpChallengeRepository;
    private readonly ITotpProvider _totpProvider;
    private readonly IJwtSigner _jwtSigner;
    private readonly IRateLimiterPort _rateLimiterPort;

    public VerifyMfaChallengeCommandHandler(
        IUserAccountRepository userAccountRepository,
        IOtpChallengeRepository otpChallengeRepository,
        ITotpProvider totpProvider,
        IJwtSigner jwtSigner,
        IRateLimiterPort rateLimiterPort)
    {
        _userAccountRepository = userAccountRepository;
        _otpChallengeRepository = otpChallengeRepository;
        _totpProvider = totpProvider;
        _jwtSigner = jwtSigner;
        _rateLimiterPort = rateLimiterPort;
    }

    public async Task<Result<LoginResultDto>> Handle(VerifyMfaChallengeCommand request, CancellationToken cancellationToken)
    {
        var canProceed = await _rateLimiterPort.TryConsumeAsync($"verify_mfa_{request.IpAddress}", 15, TimeSpan.FromMinutes(1), cancellationToken);
        if (!canProceed)
            return Result.Failure<LoginResultDto>(new Error("E-RATE-LIMITED", "Too many attempts."));

        var challenge = await _otpChallengeRepository.GetByIdAsync(new OtpChallengeId(request.ChallengeId), cancellationToken);
        if (challenge == null || challenge.Purpose != OtpPurpose.Mfa)
            return Result.Failure<LoginResultDto>(new Error("E-NOT-FOUND", "Challenge not found."));

        var account = await _userAccountRepository.GetByIdAsync(challenge.UserAccountId, cancellationToken);
        if (account == null)
            return Result.Failure<LoginResultDto>(new Error("E-NOT-FOUND", "Account not found."));

        if (!account.Mfa.Enabled)
            return Result.Failure<LoginResultDto>(new Error("E-MFA-NOT-ENABLED", "MFA is not enabled."));

        if (account.LockState.IsLocked && !account.LockState.IsExpired())
            return Result.Failure<LoginResultDto>(new Error("E-LOGIN-ACCOUNT-LOCKED", "Account is locked."));

        bool isValid = false;

        // Try as backup code first (if 8 chars or more)
        if (request.Code.Length >= 8)
        {
            var codeHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(request.Code)));
            var redeemResult = account.RedeemBackupCode(codeHash);
            if (redeemResult.IsSuccess)
                isValid = true;
        }
        else if (account.Mfa.Method == MfaMethod.Totp && account.Mfa.SecretRef != null)
        {
            isValid = _totpProvider.Verify(account.Mfa.SecretRef, request.Code);
        }
        else if (account.Mfa.Method == MfaMethod.SmsOtp)
        {
            isValid = challenge.CodeHash == request.Code;
        }

        if (!isValid)
        {
            account.RecordOtpFailure(); // Assuming incorrect OTP
            var verifyResult = challenge.Verify(Guid.NewGuid().ToString(), DateTime.UtcNow); // Force failed verification to increment AttemptCount
            
            if (account.LockState.FailedOtpCount >= 3)
            {
                account.Lock(DateTime.UtcNow.AddMinutes(15));
                return Result.Failure<LoginResultDto>(new Error("E-LOGIN-ACCOUNT-LOCKED", "Account is locked due to too many failed MFA attempts."));
            }

            return Result.Failure<LoginResultDto>(new Error("E-OTP-INVALID", "Invalid MFA code."));
        }

        challenge.Verify(challenge.CodeHash, DateTime.UtcNow); // Mark as verified successfully

        var channelResult = Enum.TryParse<Channel>(request.Channel, true, out var channel) ? channel : Channel.Web;
        var deviceFingerprintResult = DeviceFingerprint.Create(request.DeviceFingerprint);
        var fingerprint = deviceFingerprintResult.IsSuccess ? deviceFingerprintResult.Value : DeviceFingerprint.Create("unknown").Value;

        var sessionId = Guid.NewGuid();
        var (refreshToken, refreshTokenHash) = _jwtSigner.IssueRefreshToken();
        
        var accessTokenSpecResult = TokenClaimsBuilder.BuildAccessToken(account, sessionId, Array.Empty<string>(), TimeSpan.FromHours(1));
        if (accessTokenSpecResult.IsFailure) return Result.Failure<LoginResultDto>(accessTokenSpecResult.Error);

        var accessToken = _jwtSigner.SignAccessToken(accessTokenSpecResult.Value);

        var loginResult = account.RecordSuccessfulLogin(channel, fingerprint, refreshTokenHash, DateTime.UtcNow.AddDays(30));
        if (loginResult.IsFailure) return Result.Failure<LoginResultDto>(loginResult.Error);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(new LoginResultDto(accessToken, refreshToken, accessTokenSpecResult.Value.ExpiresOnUtc, false, null));
    }
}

