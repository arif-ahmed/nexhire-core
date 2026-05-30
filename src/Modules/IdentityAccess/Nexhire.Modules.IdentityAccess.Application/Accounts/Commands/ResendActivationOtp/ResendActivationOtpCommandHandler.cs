using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ResendActivationOtp;

public class ResendActivationOtpCommandHandler : ICommandHandler<ResendActivationOtpCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IOtpChallengeRepository _otpChallengeRepository;
    private readonly IRateLimiterPort _rateLimiterPort;
    private readonly IOtpDeliveryPort _otpDeliveryPort;

    public ResendActivationOtpCommandHandler(
        IUserAccountRepository userAccountRepository,
        IOtpChallengeRepository otpChallengeRepository,
        IRateLimiterPort rateLimiterPort,
        IOtpDeliveryPort otpDeliveryPort)
    {
        _userAccountRepository = userAccountRepository;
        _otpChallengeRepository = otpChallengeRepository;
        _rateLimiterPort = rateLimiterPort;
        _otpDeliveryPort = otpDeliveryPort;
    }

    public async Task<Result> Handle(ResendActivationOtpCommand request, CancellationToken cancellationToken)
    {
        var canProceed = await _rateLimiterPort.TryConsumeAsync($"resend_{request.UserId}", 3, TimeSpan.FromHours(1), cancellationToken);
        if (!canProceed)
            return Result.Failure(new Error("E-RATE-LIMITED", "Too many resend attempts."));

        var accountId = new UserAccountId(request.UserId);
        var account = await _userAccountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null)
            return Result.Failure(new Error("E-NOT-FOUND", "Account not found."));

        if (account.Status != AccountStatus.PendingActivation)
            return Result.Failure(new Error("E-ACCOUNT-NOT-PENDING", "Account is already activated or not pending."));

        var existingChallenge = await _otpChallengeRepository.GetActiveByAccountAndPurposeAsync(accountId, OtpPurpose.Activation, cancellationToken);
        if (existingChallenge != null)
        {
            existingChallenge.MarkExpired();
        }

        var code = new Random().Next(100000, 999999).ToString();
        var challenge = OtpChallenge.Issue(account.Id, OtpPurpose.Activation, code, TimeSpan.FromMinutes(5), 5);
        
        await _otpChallengeRepository.AddAsync(challenge, cancellationToken);
        await _otpDeliveryPort.SendAsync(account.Credential.Mobile.Value, code, OtpPurpose.Activation, cancellationToken);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

