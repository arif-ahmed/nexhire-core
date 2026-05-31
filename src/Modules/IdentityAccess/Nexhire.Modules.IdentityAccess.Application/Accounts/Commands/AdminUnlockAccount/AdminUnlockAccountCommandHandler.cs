using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminUnlockAccount;

public class AdminUnlockAccountCommandHandler : ICommandHandler<AdminUnlockAccountCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAdminActionLogRepository _adminActionLogRepository;

    public AdminUnlockAccountCommandHandler(
        IUserAccountRepository userAccountRepository,
        IAdminActionLogRepository adminActionLogRepository)
    {
        _userAccountRepository = userAccountRepository;
        _adminActionLogRepository = adminActionLogRepository;
    }

    public async Task<Result> Handle(AdminUnlockAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.TargetUserId), cancellationToken);
        if (account == null)
            return Result.Failure(new Error("E-NOT-FOUND", "Account not found."));

        account.Unlock();

        var log = AdminActionLog.Record(request.AdminUserId, AdminActionType.Unlocked, request.TargetUserId, null);
        await _adminActionLogRepository.AddAsync(log, cancellationToken);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

