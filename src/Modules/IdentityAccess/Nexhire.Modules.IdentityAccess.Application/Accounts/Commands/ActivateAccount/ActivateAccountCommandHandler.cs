using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ActivateAccount;

public class ActivateAccountCommandHandler : ICommandHandler<ActivateAccountCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IOtpChallengeRepository _otpChallengeRepository;

    public ActivateAccountCommandHandler(
        IUserAccountRepository userAccountRepository,
        IOtpChallengeRepository otpChallengeRepository)
    {
        _userAccountRepository = userAccountRepository;
        _otpChallengeRepository = otpChallengeRepository;
    }

    public async Task<Result> Handle(ActivateAccountCommand request, CancellationToken cancellationToken)
    {
        var accountId = new UserAccountId(request.UserId);
        var account = await _userAccountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null)
            return Result.Failure(new Error("E-NOT-FOUND", "Account not found."));

        var challenge = await _otpChallengeRepository.GetActiveByAccountAndPurposeAsync(accountId, OtpPurpose.Activation, cancellationToken);
        if (challenge == null)
            return Result.Failure(new Error("E-OTP-NOT-FOUND", "No active activation challenge found."));

        var verifyResult = challenge.Verify(request.OtpCode, DateTime.UtcNow);
        if (verifyResult.IsFailure)
        {
            if (verifyResult.Error.Code == "E-OTP-LOCKED")
                account.RecordOtpFailure(); // E-OTP-LOCKED
                
            return verifyResult;
        }

        var activateResult = account.Activate();
        if (activateResult.IsFailure)
            return activateResult;

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

