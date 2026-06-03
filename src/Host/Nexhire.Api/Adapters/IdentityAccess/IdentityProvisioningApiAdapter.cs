using MediatR;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ProvisionCredential;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Api.Adapters.IdentityAccess;

public class IdentityProvisioningApiAdapter :
    Nexhire.Modules.EmployerProfiles.Domain.Ports.IIdentityProvisioningApi,
    Nexhire.Modules.IdentityAccess.Contracts.IIdentityProvisioningApi
{
    private readonly ISender _sender;

    public IdentityProvisioningApiAdapter(ISender sender)
    {
        _sender = sender;
    }

    public async Task<Result<Nexhire.Modules.EmployerProfiles.Domain.Ports.ProvisionedIdentity>> ProvisionCredentialAsync(
        string email, string mobile, string password, string role, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ProvisionCredentialCommand(email, mobile, password, role), cancellationToken);
        if (result.IsFailure)
            return Result.Failure<Nexhire.Modules.EmployerProfiles.Domain.Ports.ProvisionedIdentity>(result.Error);

        return new Nexhire.Modules.EmployerProfiles.Domain.Ports.ProvisionedIdentity(result.Value);
    }

    public async Task<Result<Nexhire.Modules.IdentityAccess.Contracts.ProvisionedIdentity>> ProvisionCredential(
        string email, string mobile, string password, string role)
    {
        var result = await _sender.Send(new ProvisionCredentialCommand(email, mobile, password, role));
        if (result.IsFailure)
            return Result.Failure<Nexhire.Modules.IdentityAccess.Contracts.ProvisionedIdentity>(result.Error);

        return new Nexhire.Modules.IdentityAccess.Contracts.ProvisionedIdentity(result.Value);
    }
}