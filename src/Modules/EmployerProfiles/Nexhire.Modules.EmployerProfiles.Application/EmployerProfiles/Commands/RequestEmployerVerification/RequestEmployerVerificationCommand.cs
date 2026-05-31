using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.RequestEmployerVerification;

public record RequestEmployerVerificationCommand(Guid UserId, string RegistryRef) : ICommand;
