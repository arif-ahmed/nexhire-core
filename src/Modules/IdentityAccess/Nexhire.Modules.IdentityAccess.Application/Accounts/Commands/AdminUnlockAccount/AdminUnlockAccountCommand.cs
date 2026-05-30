using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminUnlockAccount;

public record AdminUnlockAccountCommand(Guid AdminUserId, Guid TargetUserId) : ICommand;
