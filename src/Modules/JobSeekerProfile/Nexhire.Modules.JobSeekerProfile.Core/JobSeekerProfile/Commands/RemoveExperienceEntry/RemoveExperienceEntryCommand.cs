using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveExperienceEntry;

public record RemoveExperienceEntryCommand(Guid UserId, Guid ExperienceEntryId) : ICommand;
