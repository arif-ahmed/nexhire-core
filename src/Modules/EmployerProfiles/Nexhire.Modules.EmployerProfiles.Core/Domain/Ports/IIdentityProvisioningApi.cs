using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;

public interface IIdentityProvisioningApi
{
    Task<Result<ProvisionedIdentity>> ProvisionCredentialAsync(string email, string mobile, string password, string role, CancellationToken cancellationToken = default);
}

public record ProvisionedIdentity(Guid UserId);
