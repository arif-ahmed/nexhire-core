using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminRejectEmployer;

public record AdminRejectEmployerCommand(Guid AdminUserId, Guid TargetUserId, string Reason) : ICommand;
