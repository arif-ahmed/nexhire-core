using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.Logout;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;

    public LogoutCommandHandler(IUserAccountRepository userAccountRepository)
    {
        _userAccountRepository = userAccountRepository;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.UserId), cancellationToken);
        if (account == null)
            return Result.Failure(new Error("E-NOT-FOUND", "Account not found."));

        account.RevokeSession(new SessionId(request.SessionId));
        await _userAccountRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

