using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.DisablePublicSharing;

public record DisablePublicSharingCommand(Guid UserId) : ICommand;
