using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using System.Security.Cryptography;
using System.Text;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.CompletePasswordReset;

public class CompletePasswordResetCommandHandler : ICommandHandler<CompletePasswordResetCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IBreachCheckPort _breachCheckPort;

    public CompletePasswordResetCommandHandler(
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        IBreachCheckPort breachCheckPort)
    {
        _userAccountRepository = userAccountRepository;
        _passwordHasher = passwordHasher;
        _breachCheckPort = breachCheckPort;
    }

    public async Task<Result> Handle(CompletePasswordResetCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.ResetToken)));
        
        var account = await _userAccountRepository.GetByPasswordResetTokenHashAsync(tokenHash, cancellationToken);
        if (account == null)
            return Result.Failure(new Error("E-RESET-INVALID-TOKEN", "Invalid token."));

        var rawPasswordResult = RawPassword.Create(request.NewPassword);
        if (rawPasswordResult.IsFailure)
            return Result.Failure(rawPasswordResult.Error with { Code = "E-RESET-INVALID-PASSWORD" });

        var rawPassword = rawPasswordResult.Value;

        var policyResult = PasswordPolicyService.Validate(rawPassword, "E-RESET");
        if (policyResult.IsFailure)
            return Result.Failure(policyResult.Error);

        if (await _breachCheckPort.IsBreachedAsync(rawPassword, cancellationToken))
            return Result.Failure(new Error("E-RESET-BREACHED-PASSWORD", "Password has appeared in a data breach."));

        if (account.IsPasswordReused(request.NewPassword))
            return Result.Failure(new Error("E-RESET-PASSWORD-REUSED", "Password was used recently."));

        var passwordHash = _passwordHasher.Hash(rawPassword);
        var result = account.CompletePasswordReset(tokenHash, passwordHash);
        if (result.IsFailure) return result;

        account.AddToPasswordHistory(request.NewPassword);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

