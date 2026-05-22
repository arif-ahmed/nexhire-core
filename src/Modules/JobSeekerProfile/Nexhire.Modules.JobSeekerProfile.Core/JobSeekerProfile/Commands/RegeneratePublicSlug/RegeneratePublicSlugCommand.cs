using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RegeneratePublicSlug;

public record RegeneratePublicSlugCommand(Guid UserId) : ICommand;
