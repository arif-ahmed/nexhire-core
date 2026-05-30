using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminIssuePasswordReset;

public record AdminIssuePasswordResetCommand(Guid AdminUserId, Guid TargetUserId) : ICommand;
