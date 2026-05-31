using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminDeactivateUser;

public record AdminDeactivateUserCommand(Guid AdminUserId, Guid TargetUserId, string Reason) : ICommand;
