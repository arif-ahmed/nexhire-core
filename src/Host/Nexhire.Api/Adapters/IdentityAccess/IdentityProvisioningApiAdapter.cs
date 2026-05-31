using MediatR;
using Nexhire.Modules.IdentityAccess.Contracts;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ProvisionCredential;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Api.Adapters.IdentityAccess;

public class IdentityProvisioningApiAdapter : IIdentityProvisioningApi
{
    private readonly ISender _sender;

    public IdentityProvisioningApiAdapter(ISender sender)
    {
        _sender = sender;
    }

    public async Task<Result<ProvisionedIdentity>> ProvisionCredential(string email, string mobile, string password, string role)
    {
        var result = await _sender.Send(new ProvisionCredentialCommand(email, mobile, password, role));
        if (result.IsFailure) return Result.Failure<ProvisionedIdentity>(result.Error);

        return new ProvisionedIdentity(result.Value);
    }
}
