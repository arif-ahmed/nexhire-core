using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using System.Security.Cryptography;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminIssuePasswordReset;

public class AdminIssuePasswordResetCommandHandler : ICommandHandler<AdminIssuePasswordResetCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAdminActionLogRepository _adminActionLogRepository;

    public AdminIssuePasswordResetCommandHandler(
        IUserAccountRepository userAccountRepository,
        IAdminActionLogRepository adminActionLogRepository)
    {
        _userAccountRepository = userAccountRepository;
        _adminActionLogRepository = adminActionLogRepository;
    }

    public async Task<Result> Handle(AdminIssuePasswordResetCommand request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.TargetUserId), cancellationToken);
        if (account == null)
            return Result.Failure(new Error("E-NOT-FOUND", "Account not found."));

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
        
        account.IssuePasswordResetToken(tokenHash, DateTime.UtcNow.AddHours(24));

        var log = AdminActionLog.Record(request.AdminUserId, AdminActionType.PasswordResetIssued, request.TargetUserId, null);
        await _adminActionLogRepository.AddAsync(log, cancellationToken);

        // Note: we don't return the rawToken to admin. The domain event `PasswordResetIntegrationEvent` will trigger a notification.
        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

