using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.RefreshAccessToken;

public record RefreshAccessTokenCommand(string RefreshToken) : ICommand<LoginResultDto>;
