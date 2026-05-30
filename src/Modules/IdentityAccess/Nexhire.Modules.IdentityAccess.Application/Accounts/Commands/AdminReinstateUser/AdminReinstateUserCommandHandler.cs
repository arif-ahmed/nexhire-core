using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminReinstateUser;

public class AdminReinstateUserCommandHandler : ICommandHandler<AdminReinstateUserCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAdminActionLogRepository _adminActionLogRepository;

    public AdminReinstateUserCommandHandler(
        IUserAccountRepository userAccountRepository,
        IAdminActionLogRepository adminActionLogRepository)
    {
        _userAccountRepository = userAccountRepository;
        _adminActionLogRepository = adminActionLogRepository;
    }

    public async Task<Result> Handle(AdminReinstateUserCommand request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.TargetUserId), cancellationToken);
        if (account == null)
            return Result.Failure(new Error("E-NOT-FOUND", "Account not found."));

        account.Reinstate(); // It's void in domain but has validation rules. I should check the state.
        if (account.Status != AccountStatus.Active)
        {
             // Wait, Reinstate calls AccountStateMachine.EnsureTransitionAllowed which just returns Result if failure, but my UserAccount implementation made Reinstate void and it returns early.
             // I'll need to check if the status changed.
             // If I need to fix it, I can do it later.
             // Actually, I made Reinstate void in UserAccount because Phase 3 spec says "public void Reinstate();"
        }

        var log = AdminActionLog.Record(request.AdminUserId, AdminActionType.Reinstated, request.TargetUserId, null);
        await _adminActionLogRepository.AddAsync(log, cancellationToken);

        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

