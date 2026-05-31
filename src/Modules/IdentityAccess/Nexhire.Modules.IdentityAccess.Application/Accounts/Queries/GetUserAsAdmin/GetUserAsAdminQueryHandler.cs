using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetUserAsAdmin;

public class GetUserAsAdminQueryHandler : IQueryHandler<GetUserAsAdminQuery, AdminUserDetailDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAdminActionLogRepository _adminActionLogRepository;

    public GetUserAsAdminQueryHandler(
        IUserAccountRepository userAccountRepository,
        IAdminActionLogRepository adminActionLogRepository)
    {
        _userAccountRepository = userAccountRepository;
        _adminActionLogRepository = adminActionLogRepository;
    }

    public async Task<Result<AdminUserDetailDto>> Handle(GetUserAsAdminQuery request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.TargetUserId), cancellationToken);
        if (account == null)
            return Result.Failure<AdminUserDetailDto>(new Error("E-NOT-FOUND", "Account not found."));

        // Log the view action
        var log = AdminActionLog.Record(request.AdminUserId, AdminActionType.Viewed, request.TargetUserId, null);
        await _adminActionLogRepository.AddAsync(log, cancellationToken);

        var dto = new AdminUserDetailDto(
            account.Id.Value,
            account.Credential.Email.Value,
            account.Credential.Mobile?.Value ?? "",
            account.Role.ToString(),
            account.Status.ToString(),
            false, // identityVerified
            account.LockState.IsLocked && !account.LockState.IsExpired(),
            account.LockState.LockedUntilUtc,
            account.LockState.FailedLoginCount,
            account.LockState.FailedOtpCount,
            account.Sessions.Count
        );

        return Result.Success(dto);
    }
}
