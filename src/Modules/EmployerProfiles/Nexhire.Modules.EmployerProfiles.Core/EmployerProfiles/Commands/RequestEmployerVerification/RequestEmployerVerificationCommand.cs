using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RequestEmployerVerification;

public record RequestEmployerVerificationCommand(Guid UserId, string RegistryRef) : ICommand;
