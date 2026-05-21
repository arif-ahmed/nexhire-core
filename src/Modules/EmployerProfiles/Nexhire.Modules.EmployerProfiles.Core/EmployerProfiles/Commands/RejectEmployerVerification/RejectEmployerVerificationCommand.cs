using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RejectEmployerVerification;

public record RejectEmployerVerificationCommand(Guid ProfileId, Guid AdminId, string Reason) : ICommand;
