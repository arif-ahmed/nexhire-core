using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Adapters;

public class StubIdentityProvisioningApi : IIdentityProvisioningApi
{
    public Task<Result<ProvisionedIdentity>> ProvisionCredentialAsync(
        string email,
        string mobile,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        // Simple stub that provisions credentials and returns a new mock UserId
        var provisioned = new ProvisionedIdentity(Guid.NewGuid());
        return Task.FromResult(Result.Success(provisioned));
    }
}
