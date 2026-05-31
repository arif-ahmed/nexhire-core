using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Queries.ValidateToken;

public class ValidateTokenQueryHandler : IQueryHandler<ValidateTokenQuery, ValidatedPrincipal>
{
    private readonly IJwtSigner _jwtSigner;

    public ValidateTokenQueryHandler(IJwtSigner jwtSigner)
    {
        _jwtSigner = jwtSigner;
    }

    public async Task<Result<ValidatedPrincipal>> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        bool isValid = _jwtSigner.ValidateSignature(request.AccessToken);
        if (!isValid)
            return Result.Failure<ValidatedPrincipal>(new Error("E-INVALID-TOKEN", "The token signature is invalid or expired."));

        // Normally we'd decode the JWT to extract these claims here.
        // For now, this is a placeholder response since IJwtSigner interface only validates the signature.
        // A full implementation would return the extracted claims.
        return Result.Success(new ValidatedPrincipal(Guid.NewGuid(), "User", new List<string>(), Guid.NewGuid()));
    }
}
