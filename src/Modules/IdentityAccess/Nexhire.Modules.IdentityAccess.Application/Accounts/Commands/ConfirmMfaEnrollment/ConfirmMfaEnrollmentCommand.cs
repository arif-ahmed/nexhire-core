using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ConfirmMfaEnrollment;

public record ConfirmMfaEnrollmentCommand(Guid UserId, string Code, string Method) : ICommand<ConfirmMfaEnrollmentResultDto>;
