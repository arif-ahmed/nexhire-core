using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RestoreProfileVersion;

public record RestoreProfileVersionCommand(Guid UserId, Guid VersionId) : ICommand;
