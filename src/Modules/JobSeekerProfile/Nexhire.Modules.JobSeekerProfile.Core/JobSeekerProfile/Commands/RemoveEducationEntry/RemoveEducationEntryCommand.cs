using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveEducationEntry;

public record RemoveEducationEntryCommand(Guid UserId, Guid EducationEntryId) : ICommand;
