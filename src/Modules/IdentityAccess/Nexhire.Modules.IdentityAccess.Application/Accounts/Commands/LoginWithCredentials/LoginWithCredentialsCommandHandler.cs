using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.LoginWithCredentials;

public class LoginWithCredentialsCommandHandler : ICommandHandler<LoginWithCredentialsCommand, LoginResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IOtpChallengeRepository _otpChallengeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtSigner _jwtSigner;
    private readonly IRateLimiterPort _rateLimiterPort;
    private readonly IOtpDeliveryPort _otpDeliveryPort;

    public LoginWithCredentialsCommandHandler(
        IUserAccountRepository userAccountRepository,
        IOtpChallengeRepository otpChallengeRepository,
        IPasswordHasher passwordHasher,
        IJwtSigner jwtSigner,
        IRateLimiterPort rateLimiterPort,
        IOtpDeliveryPort otpDeliveryPort)
    {
        _userAccountRepository = userAccountRepository;
        _otpChallengeRepository = otpChallengeRepository;
        _passwordHasher = passwordHasher;
        _jwtSigner = jwtSigner;
        _rateLimiterPort = rateLimiterPort;
        _otpDeliveryPort = otpDeliveryPort;
    }

    public async Task<Result<LoginResultDto>> Handle(LoginWithCredentialsCommand request, CancellationToken cancellationToken)
    {
        var canProceed = await _rateLimiterPort.TryConsumeAsync($"login_{request.IpAddress}", 10, TimeSpan.FromMinutes(1), cancellationToken);
        if (!canProceed)
            return Result.Failure<LoginResultDto>(new Error("E-RATE-LIMITED", "Too many login attempts."));

        var account = await _userAccountRepository.GetByEmailOrMobileAsync(request.Identifier, cancellationToken);
        if (account == null)
            return Result.Failure<LoginResultDto>(new Error("E-LOGIN-INVALID-CREDENTIALS", "Invalid credentials."));

        var rawPasswordResult = RawPassword.Create(request.Password);
        if (rawPasswordResult.IsFailure || !_passwordHasher.Verify(rawPasswordResult.Value, account.Credential.PasswordHash))
        {
            account.RecordFailedLogin();
            return Result.Failure<LoginResultDto>(new Error("E-LOGIN-INVALID-CREDENTIALS", "Invalid credentials."));
        }

        if (account.Status != AccountStatus.Active)
            return Result.Failure<LoginResultDto>(new Error($"E-LOGIN-ACCOUNT-{account.Status.ToString().ToUpper()}", $"Account is {account.Status}."));

        if (account.LockState.IsLocked && !account.LockState.IsExpired())
            return Result.Failure<LoginResultDto>(new Error("E-LOGIN-ACCOUNT-LOCKED", "Account is locked."));

        if (account.Mfa.Enabled)
        {
            var code = new Random().Next(100000, 999999).ToString();
            var challenge = OtpChallenge.Issue(account.Id, OtpPurpose.Mfa, code, TimeSpan.FromMinutes(5), 3);
            await _otpChallengeRepository.AddAsync(challenge, cancellationToken);
            
            if (account.Mfa.Method == MfaMethod.SmsOtp)
            {
                await _otpDeliveryPort.SendAsync(account.Credential.Mobile.Value, code, OtpPurpose.Mfa, cancellationToken);
            }

            await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(new LoginResultDto(string.Empty, string.Empty, DateTime.MinValue, true, challenge.Id.Value));
        }

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

