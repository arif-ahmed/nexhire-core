using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.IssueOAuthToken;

public class IssueOAuthTokenCommandHandler : ICommandHandler<IssueOAuthTokenCommand, LoginResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IJwtSigner _jwtSigner;

    public IssueOAuthTokenCommandHandler(
        IUserAccountRepository userAccountRepository,
        IJwtSigner jwtSigner)
    {
        _userAccountRepository = userAccountRepository;
        _jwtSigner = jwtSigner;
    }

    public async Task<Result<LoginResultDto>> Handle(IssueOAuthTokenCommand request, CancellationToken cancellationToken)
    {
        // Dummy implementation for OAuth Token issuance
        // In reality, this would validate the client, check the authorization code/PKCE, and find the corresponding user.
        // For Client-Credentials, it might issue a token for a system account.

        if (request.GrantType == "client_credentials")
        {
            // Just returning a dummy error or we could generate a token if we had a system account.
            return Result.Failure<LoginResultDto>(new Error("E-OAUTH-UNSUPPORTED", "Client credentials not fully implemented yet."));
        }

        if (request.GrantType == "authorization_code")
        {
            // E.g., we'd look up the auth code from a repository. For now, failure.
            return Result.Failure<LoginResultDto>(new Error("E-OAUTH-UNSUPPORTED", "Authorization code flow not fully implemented yet."));
        }

        return Result.Failure<LoginResultDto>(new Error("E-OAUTH-UNSUPPORTED-GRANT", "Unsupported grant type."));
    }
}

