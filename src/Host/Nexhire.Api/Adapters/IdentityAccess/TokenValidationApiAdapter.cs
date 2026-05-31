using MediatR;
using Nexhire.Modules.IdentityAccess.Contracts;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.ValidateToken;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Api.Adapters.IdentityAccess;

public class TokenValidationApiAdapter : ITokenValidationApi
{
    private readonly ISender _sender;

    public TokenValidationApiAdapter(ISender sender)
    {
        _sender = sender;
    }

    public async Task<Result<ValidatedPrincipal>> Validate(string accessToken)
    {
        var result = await _sender.Send(new ValidateTokenQuery(accessToken));
        if (result.IsFailure) return Result.Failure<ValidatedPrincipal>(result.Error);

        return new ValidatedPrincipal(
            result.Value.UserId,
            result.Value.Role,
            result.Value.Permissions,
            result.Value.SessionId);
    }
}
