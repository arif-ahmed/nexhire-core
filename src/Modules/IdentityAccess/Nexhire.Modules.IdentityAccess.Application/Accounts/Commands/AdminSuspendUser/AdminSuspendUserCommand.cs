using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminSuspendUser;

public record AdminSuspendUserCommand(Guid AdminUserId, Guid TargetUserId, string Reason) : ICommand;
