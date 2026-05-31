using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AssignRole;

public record AssignRoleCommand(Guid AdminUserId, Guid TargetUserId, string Role) : ICommand;
