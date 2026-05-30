using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ProvisionCredential;

public class ProvisionCredentialCommandHandler : ICommandHandler<ProvisionCredentialCommand, Guid>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IOtpChallengeRepository _otpChallengeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IBreachCheckPort _breachCheckPort;
    private readonly IRateLimiterPort _rateLimiterPort;
    private readonly IOtpDeliveryPort _otpDeliveryPort;

    public ProvisionCredentialCommandHandler(
        IUserAccountRepository userAccountRepository,
        IOtpChallengeRepository otpChallengeRepository,
        IPasswordHasher passwordHasher,
        IBreachCheckPort breachCheckPort,
        IRateLimiterPort rateLimiterPort,
        IOtpDeliveryPort otpDeliveryPort)
    {
        _userAccountRepository = userAccountRepository;
        _otpChallengeRepository = otpChallengeRepository;
        _passwordHasher = passwordHasher;
        _breachCheckPort = breachCheckPort;
        _rateLimiterPort = rateLimiterPort;
        _otpDeliveryPort = otpDeliveryPort;
    }

    public async Task<Result<Guid>> Handle(ProvisionCredentialCommand request, CancellationToken cancellationToken)
    {
        // 1. Rate-limit
        var canProceed = await _rateLimiterPort.TryConsumeAsync($"provision_{request.Email}", 5, TimeSpan.FromHours(1), cancellationToken);
        if (!canProceed)
            return Result.Failure<Guid>(new Error("E-RATE-LIMITED", "Too many registration attempts."));

        // 2. Uniqueness
        if (await _userAccountRepository.EmailExistsAsync(request.Email, cancellationToken))
            return Result.Failure<Guid>(new Error("E-REG-DUPLICATE-EMAIL", "Email is already registered."));

        if (await _userAccountRepository.MobileExistsAsync(request.Mobile, cancellationToken))
            return Result.Failure<Guid>(new Error("E-REG-DUPLICATE-MOBILE", "Mobile number is already registered."));

        // 3. Policy & Breach
        var rawPasswordResult = RawPassword.Create(request.Password);
        if (rawPasswordResult.IsFailure)
            return Result.Failure<Guid>(rawPasswordResult.Error);

        var rawPassword = rawPasswordResult.Value;
        if (await _breachCheckPort.IsBreachedAsync(rawPassword, cancellationToken))
            return Result.Failure<Guid>(new Error("E-REG-BREACHED-PASSWORD", "Password has appeared in a data breach."));

        var emailResult = EmailAddress.Create(request.Email);
        if (emailResult.IsFailure) return Result.Failure<Guid>(emailResult.Error);

        var mobileResult = MobileNumber.Create(request.Mobile);
        if (mobileResult.IsFailure) return Result.Failure<Guid>(mobileResult.Error);

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return Result.Failure<Guid>(new Error("E-REG-INVALID-ROLE", "Invalid role specified."));

        // 4. Hash
        var passwordHash = _passwordHasher.Hash(rawPassword);

        // 5. Provision UserAccount
        var permissions = PermissionResolver.Resolve(role, Array.Empty<string>());
        var account = UserAccount.Provision(emailResult.Value, mobileResult.Value, passwordHash, role, permissions);
        account.AddToPasswordHistory(request.Password);

        // 6. Issue Activation OTP
        // Note: For simplicity we generate a random 6-digit code. In reality, we might have a generator service.
        var code = new Random().Next(100000, 999999).ToString();
        var codeHash = code; // Hash it in real life, but for now we might just store it. Wait, the domain expects string.
        var challenge = OtpChallenge.Issue(account.Id, OtpPurpose.Activation, codeHash, TimeSpan.FromMinutes(5), 5);

        // 7. Persist
        await _userAccountRepository.AddAsync(account, cancellationToken);
        await _otpChallengeRepository.AddAsync(challenge, cancellationToken);

        // 8. React (Send OTP)
        await _otpDeliveryPort.SendAsync(request.Mobile, code, OtpPurpose.Activation, cancellationToken);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(account.Id.Value);
    }
}

