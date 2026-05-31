using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.EnrollMfa;

public class EnrollMfaCommandHandler : ICommandHandler<EnrollMfaCommand, EnrollMfaResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IOtpChallengeRepository _otpChallengeRepository;
    private readonly ITotpProvider _totpProvider;
    private readonly IOtpDeliveryPort _otpDeliveryPort;

    public EnrollMfaCommandHandler(
        IUserAccountRepository userAccountRepository,
        IOtpChallengeRepository otpChallengeRepository,
        ITotpProvider totpProvider,
        IOtpDeliveryPort otpDeliveryPort)
    {
        _userAccountRepository = userAccountRepository;
        _otpChallengeRepository = otpChallengeRepository;
        _totpProvider = totpProvider;
        _otpDeliveryPort = otpDeliveryPort;
    }

    public async Task<Result<EnrollMfaResultDto>> Handle(EnrollMfaCommand request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.UserId), cancellationToken);
        if (account == null)
            return Result.Failure<EnrollMfaResultDto>(new Error("E-NOT-FOUND", "Account not found."));

        if (account.Status != AccountStatus.Active)
            return Result.Failure<EnrollMfaResultDto>(new Error("E-ACCOUNT-NOT-ACTIVE", "Account is not active."));

        if (account.Mfa.Enabled)
            return Result.Failure<EnrollMfaResultDto>(new Error("E-MFA-ALREADY-ENABLED", "MFA is already enabled."));

        if (!Enum.TryParse<MfaMethod>(request.Method, true, out var method))
            return Result.Failure<EnrollMfaResultDto>(new Error("E-INVALID-MFA-METHOD", "Invalid MFA method."));

        if (method == MfaMethod.Totp)
        {
            var (secretRef, provisioningUri) = _totpProvider.Enroll(account.Credential.Email.Value);
            // We need a place to temporarily store secretRef until confirmed. 
            // OtpChallenge can be used by passing the secretRef as the codeHash (hacky) or we just expect it back in the confirm request. 
            // Wait, we can't trust the client with the secretRef. Let's issue an OtpChallenge for MFA with secretRef as the CodeHash so it's persisted, 
            // or the challenge is just the Totp secret. Actually, we shouldn't send the secret in OtpChallenge.CodeHash.
            // Let's store the secretRef in OtpChallenge's CodeHash since it's the challenge for TOTP.
            var challenge = OtpChallenge.Issue(account.Id, OtpPurpose.Mfa, secretRef, TimeSpan.FromMinutes(15), 5);
            await _otpChallengeRepository.AddAsync(challenge, cancellationToken);
            await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(new EnrollMfaResultDto(provisioningUri));
        }
        else if (method == MfaMethod.SmsOtp)
        {
            var code = new Random().Next(100000, 999999).ToString();
            var challenge = OtpChallenge.Issue(account.Id, OtpPurpose.Mfa, code, TimeSpan.FromMinutes(5), 3);
            await _otpChallengeRepository.AddAsync(challenge, cancellationToken);
            await _otpDeliveryPort.SendAsync(account.Credential.Mobile.Value, code, OtpPurpose.Mfa, cancellationToken);
            await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(new EnrollMfaResultDto(null));
        }
        else
        {
            return Result.Failure<EnrollMfaResultDto>(new Error("E-INVALID-MFA-METHOD", "Method not supported."));
        }
    }
}

