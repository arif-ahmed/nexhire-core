using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ActivateAccount;

public record ActivateAccountCommand(Guid UserId, string OtpCode) : ICommand;
