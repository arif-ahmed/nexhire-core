using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Contracts;

public interface IIdentityProvisioningApi
{
    Task<Result<ProvisionedIdentity>> ProvisionCredential(
        string email, string mobile, string password, string role);
}

public record ProvisionedIdentity(Guid UserId);
