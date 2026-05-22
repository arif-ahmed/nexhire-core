using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Adapters;

public class StubIdentityProvisioningApi : IIdentityProvisioningApi
{
    public Task<Result<ProvisionedIdentity>> ProvisionCredentialAsync(
        string email,
        string mobile,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        var provisioned = new ProvisionedIdentity(Guid.NewGuid());
        return Task.FromResult(Result.Success(provisioned));
    }
}
