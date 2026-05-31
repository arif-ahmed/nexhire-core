using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminApproveEmployer;

public record AdminApproveEmployerCommand(Guid AdminUserId, Guid TargetUserId) : ICommand;
