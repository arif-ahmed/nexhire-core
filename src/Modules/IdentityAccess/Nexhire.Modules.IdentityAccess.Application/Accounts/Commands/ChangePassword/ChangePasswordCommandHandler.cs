using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ChangePassword;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IBreachCheckPort _breachCheckPort;

    public ChangePasswordCommandHandler(
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        IBreachCheckPort breachCheckPort)
    {
        _userAccountRepository = userAccountRepository;
        _passwordHasher = passwordHasher;
        _breachCheckPort = breachCheckPort;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.UserId), cancellationToken);
        if (account == null)
            return Result.Failure(new Error("E-NOT-FOUND", "Account not found."));

        var currentPasswordResult = RawPassword.Create(request.CurrentPassword);
        if (currentPasswordResult.IsFailure || !_passwordHasher.Verify(currentPasswordResult.Value, account.Credential.PasswordHash))
            return Result.Failure(new Error("E-CHANGE-INVALID-CURRENT", "Invalid current password."));

        var newPasswordResult = RawPassword.Create(request.NewPassword);
        if (newPasswordResult.IsFailure)
            return Result.Failure(newPasswordResult.Error);

        var newPassword = newPasswordResult.Value;
        if (await _breachCheckPort.IsBreachedAsync(newPassword, cancellationToken))
            return Result.Failure(new Error("E-CHANGE-BREACHED-PASSWORD", "Password has appeared in a data breach."));

        if (account.IsPasswordReused(request.NewPassword))
            return Result.Failure(new Error("E-CHANGE-PASSWORD-REUSED", "Password was used recently."));

        var passwordHash = _passwordHasher.Hash(newPassword);
        var result = account.ChangePassword(passwordHash);
        if (result.IsFailure) return result;

        account.AddToPasswordHistory(request.NewPassword);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

