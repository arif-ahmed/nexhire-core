using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Contracts;

public interface ITokenValidationApi
{
    Task<Result<ValidatedPrincipal>> Validate(string accessToken);
}

public record ValidatedPrincipal(Guid UserId, string Role, IReadOnlyList<string> Permissions, Guid SessionId);
