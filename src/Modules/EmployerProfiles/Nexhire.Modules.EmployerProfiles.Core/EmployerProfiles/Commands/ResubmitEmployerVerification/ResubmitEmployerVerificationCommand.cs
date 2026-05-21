using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.ResubmitEmployerVerification;

public record ResubmitEmployerVerificationCommand(Guid UserId) : ICommand;
