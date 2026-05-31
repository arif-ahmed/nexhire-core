using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using System.Security.Cryptography;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.VerifyPasswordResetOtp;

public class VerifyPasswordResetOtpCommandHandler : ICommandHandler<VerifyPasswordResetOtpCommand, VerifyPasswordResetOtpResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IOtpChallengeRepository _otpChallengeRepository;
    private readonly IRateLimiterPort _rateLimiterPort;

    public VerifyPasswordResetOtpCommandHandler(
        IUserAccountRepository userAccountRepository,
        IOtpChallengeRepository otpChallengeRepository,
        IRateLimiterPort rateLimiterPort)
    {
        _userAccountRepository = userAccountRepository;
        _otpChallengeRepository = otpChallengeRepository;
        _rateLimiterPort = rateLimiterPort;
    }

    public async Task<Result<VerifyPasswordResetOtpResultDto>> Handle(VerifyPasswordResetOtpCommand request, CancellationToken cancellationToken)
    {
        var canProceed = await _rateLimiterPort.TryConsumeAsync($"verify_reset_{request.Identifier}", 5, TimeSpan.FromMinutes(1), cancellationToken);
        if (!canProceed)
            return Result.Failure<VerifyPasswordResetOtpResultDto>(new Error("E-RATE-LIMITED", "Too many attempts."));

        var account = await _userAccountRepository.GetByEmailOrMobileAsync(request.Identifier, cancellationToken);
        if (account == null)
            return Result.Failure<VerifyPasswordResetOtpResultDto>(new Error("E-NOT-FOUND", "Account not found."));

        var challenge = await _otpChallengeRepository.GetActiveByAccountAndPurposeAsync(account.Id, OtpPurpose.PasswordReset, cancellationToken);
        if (challenge == null)
            return Result.Failure<VerifyPasswordResetOtpResultDto>(new Error("E-OTP-NOT-FOUND", "No active password reset challenge found."));

        var verifyResult = challenge.Verify(request.Code, DateTime.UtcNow);
        if (verifyResult.IsFailure)
        {
            if (verifyResult.Error.Code == "E-OTP-LOCKED")
                account.RecordOtpFailure(); // E-OTP-LOCKED
                
            return Result.Failure<VerifyPasswordResetOtpResultDto>(verifyResult.Error);
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
        
        account.IssuePasswordResetToken(tokenHash, DateTime.UtcNow.AddMinutes(15));

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(new VerifyPasswordResetOtpResultDto(rawToken));
    }
}

