using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminSuspendUser;

public class AdminSuspendUserCommandHandler : ICommandHandler<AdminSuspendUserCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAdminActionLogRepository _adminActionLogRepository;

    public AdminSuspendUserCommandHandler(
        IUserAccountRepository userAccountRepository,
        IAdminActionLogRepository adminActionLogRepository)
    {
        _userAccountRepository = userAccountRepository;
        _adminActionLogRepository = adminActionLogRepository;
    }

    public async Task<Result> Handle(AdminSuspendUserCommand request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.TargetUserId), cancellationToken);
        if (account == null)
            return Result.Failure(new Error("E-NOT-FOUND", "Account not found."));

        var result = account.Suspend(request.Reason);
        if (result.IsFailure) return result;

        var log = AdminActionLog.Record(request.AdminUserId, AdminActionType.Suspended, request.TargetUserId, request.Reason);
        await _adminActionLogRepository.AddAsync(log, cancellationToken);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

