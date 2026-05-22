using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.EnablePublicSharing;

public record EnablePublicSharingCommand(Guid UserId) : ICommand;
