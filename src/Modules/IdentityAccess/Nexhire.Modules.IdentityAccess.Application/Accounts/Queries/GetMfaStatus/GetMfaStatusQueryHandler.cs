using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetMfaStatus;

public class GetMfaStatusQueryHandler : IQueryHandler<GetMfaStatusQuery, MfaStatusDto>
{
    private readonly IUserAccountRepository _userAccountRepository;

    public GetMfaStatusQueryHandler(IUserAccountRepository userAccountRepository)
    {
        _userAccountRepository = userAccountRepository;
    }

    public async Task<Result<MfaStatusDto>> Handle(GetMfaStatusQuery request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.UserId), cancellationToken);
        if (account == null)
            return Result.Failure<MfaStatusDto>(new Error("E-NOT-FOUND", "Account not found."));

        var dto = new MfaStatusDto(
            account.Mfa.Enabled,
            account.Mfa.Enabled ? account.Mfa.Method.ToString() : null,
            null, // LastVerifiedUtc not strictly tracked on aggregate root yet
            account.BackupCodes.Count(b => !b.IsUsed)
        );

        return Result.Success(dto);
    }
}
