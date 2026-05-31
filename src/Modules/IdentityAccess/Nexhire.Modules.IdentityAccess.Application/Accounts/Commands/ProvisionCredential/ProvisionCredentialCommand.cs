using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ProvisionCredential;

public record ProvisionCredentialCommand(
    string Email,
    string Mobile,
    string Password,
    string Role) : ICommand<Guid>;
