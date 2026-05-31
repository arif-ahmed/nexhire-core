using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminReinstateUser;

public record AdminReinstateUserCommand(Guid AdminUserId, Guid TargetUserId) : ICommand;
