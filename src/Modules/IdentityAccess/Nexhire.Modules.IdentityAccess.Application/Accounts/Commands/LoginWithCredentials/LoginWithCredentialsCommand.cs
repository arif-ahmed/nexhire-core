using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.LoginWithCredentials;

public record LoginWithCredentialsCommand(
    string Identifier,
    string Password,
    string Channel,
    string DeviceFingerprint,
    string IpAddress) : ICommand<LoginResultDto>;
