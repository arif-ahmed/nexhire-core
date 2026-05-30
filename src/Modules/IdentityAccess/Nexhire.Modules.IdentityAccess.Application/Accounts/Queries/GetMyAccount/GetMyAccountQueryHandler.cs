using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.GetMyAccount;

public class GetMyAccountQueryHandler : IQueryHandler<GetMyAccountQuery, AccountDto>
{
    private readonly IUserAccountRepository _userAccountRepository;

    public GetMyAccountQueryHandler(IUserAccountRepository userAccountRepository)
    {
        _userAccountRepository = userAccountRepository;
    }

    public async Task<Result<AccountDto>> Handle(GetMyAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.UserId), cancellationToken);
        if (account == null)
            return Result.Failure<AccountDto>(new Error("E-NOT-FOUND", "Account not found."));

        string? mobileMasked = null;
        if (account.Credential.Mobile != null)
        {
            var mobile = account.Credential.Mobile.Value;
            if (mobile.Length > 4)
            {
                mobileMasked = mobile.Substring(0, 4) + new string('*', mobile.Length - 6) + mobile.Substring(mobile.Length - 2);
            }
        }

        var dto = new AccountDto(
            account.Id.Value,
            account.Credential.Email.Value,
            mobileMasked,
            account.Role.ToString(),
            account.Status.ToString(),
            account.Mfa.Enabled,
            false // identityVerified - for now hardcode to false or implement later
        );

        return Result.Success(dto);
    }
}
