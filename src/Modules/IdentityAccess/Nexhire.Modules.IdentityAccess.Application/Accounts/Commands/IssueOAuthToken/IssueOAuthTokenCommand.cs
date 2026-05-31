using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.IssueOAuthToken;

public record IssueOAuthTokenCommand(
    string GrantType,
    string ClientId,
    string? ClientSecret,
    string? Code,
    string? CodeVerifier,
    string? RedirectUri) : ICommand<LoginResultDto>;
