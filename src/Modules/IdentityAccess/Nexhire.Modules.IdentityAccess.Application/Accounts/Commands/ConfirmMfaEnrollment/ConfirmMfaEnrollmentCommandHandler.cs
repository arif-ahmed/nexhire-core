using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using System.Security.Cryptography;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ConfirmMfaEnrollment;

public class ConfirmMfaEnrollmentCommandHandler : ICommandHandler<ConfirmMfaEnrollmentCommand, ConfirmMfaEnrollmentResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IOtpChallengeRepository _otpChallengeRepository;
    private readonly ITotpProvider _totpProvider;

    public ConfirmMfaEnrollmentCommandHandler(
        IUserAccountRepository userAccountRepository,
        IOtpChallengeRepository otpChallengeRepository,
        ITotpProvider totpProvider)
    {
        _userAccountRepository = userAccountRepository;
        _otpChallengeRepository = otpChallengeRepository;
        _totpProvider = totpProvider;
    }

    public async Task<Result<ConfirmMfaEnrollmentResultDto>> Handle(ConfirmMfaEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.UserId), cancellationToken);
        if (account == null)
            return Result.Failure<ConfirmMfaEnrollmentResultDto>(new Error("E-NOT-FOUND", "Account not found."));

        if (!Enum.TryParse<MfaMethod>(request.Method, true, out var method))
            return Result.Failure<ConfirmMfaEnrollmentResultDto>(new Error("E-INVALID-MFA-METHOD", "Invalid MFA method."));

        var challenge = await _otpChallengeRepository.GetActiveByAccountAndPurposeAsync(account.Id, OtpPurpose.Mfa, cancellationToken);
        if (challenge == null)
            return Result.Failure<ConfirmMfaEnrollmentResultDto>(new Error("E-MFA-CHALLENGE-NOT-FOUND", "No active MFA enrollment found."));

        bool isValid = false;
        string secretRef = challenge.CodeHash; // the stored reference

        if (method == MfaMethod.Totp)
        {
            isValid = _totpProvider.Verify(secretRef, request.Code);
        }
        else if (method == MfaMethod.SmsOtp)
        {
            isValid = challenge.CodeHash == request.Code;
        }

        if (!isValid)
        {
            account.RecordOtpFailure(); // Assuming incorrect OTP
            return Result.Failure<ConfirmMfaEnrollmentResultDto>(new Error("E-OTP-INVALID", "Invalid OTP code."));
        }

        challenge.Verify(challenge.CodeHash, DateTime.UtcNow);

        // Generate 10 backup codes
        var plainCodes = new List<string>();
        var hashes = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var rawCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)); // 8 chars hex
            plainCodes.Add(rawCode);
            // using simple SHA256 for backup code hash
            var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawCode)));
            hashes.Add(hash);
        }

        var enableResult = account.EnableMfa(method, secretRef, hashes);
        if (enableResult.IsFailure)
            return Result.Failure<ConfirmMfaEnrollmentResultDto>(enableResult.Error);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(new ConfirmMfaEnrollmentResultDto(plainCodes));
    }
}

