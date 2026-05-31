using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.RequestPasswordReset;

public class RequestPasswordResetCommandHandler : ICommandHandler<RequestPasswordResetCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IOtpChallengeRepository _otpChallengeRepository;
    private readonly IRateLimiterPort _rateLimiterPort;
    private readonly IOtpDeliveryPort _otpDeliveryPort;

    public RequestPasswordResetCommandHandler(
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

    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var canProceed = await _rateLimiterPort.TryConsumeAsync($"reset_{request.Identifier}", 3, TimeSpan.FromHours(1), cancellationToken);
        if (!canProceed)
            return Result.Failure(new Error("E-RATE-LIMITED", "Too many password reset requests."));

        var account = await _userAccountRepository.GetByEmailOrMobileAsync(request.Identifier, cancellationToken);
        if (account == null)
            await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(); // Always return success for no enumeration

        var existingChallenge = await _otpChallengeRepository.GetActiveByAccountAndPurposeAsync(account.Id, OtpPurpose.PasswordReset, cancellationToken);
        if (existingChallenge != null)
        {
            existingChallenge.MarkExpired();
        }

        var code = new Random().Next(100000, 999999).ToString();
        var challenge = OtpChallenge.Issue(account.Id, OtpPurpose.PasswordReset, code, TimeSpan.FromMinutes(5), 3);
        
        await _otpChallengeRepository.AddAsync(challenge, cancellationToken);
        await _otpDeliveryPort.SendAsync(account.Credential.Mobile.Value, code, OtpPurpose.PasswordReset, cancellationToken);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

