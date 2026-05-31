using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.ApproveEmployerVerification;

public record ApproveEmployerVerificationCommand(Guid ProfileId, Guid AdminId, string EvidenceRef) : ICommand;
