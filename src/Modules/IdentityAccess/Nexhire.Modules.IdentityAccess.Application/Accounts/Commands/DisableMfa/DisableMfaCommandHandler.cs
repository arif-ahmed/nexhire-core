using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.DisableMfa;

public class DisableMfaCommandHandler : ICommandHandler<DisableMfaCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;

    public DisableMfaCommandHandler(IUserAccountRepository userAccountRepository)
    {
        _userAccountRepository = userAccountRepository;
    }

    public async Task<Result> Handle(DisableMfaCommand request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.UserId), cancellationToken);
        if (account == null)
            return Result.Failure(new Error("E-NOT-FOUND", "Account not found."));

        if (!account.Mfa.Enabled)
            return Result.Failure(new Error("E-MFA-NOT-ENABLED", "MFA is not enabled."));

        account.DisableMfa();
        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

