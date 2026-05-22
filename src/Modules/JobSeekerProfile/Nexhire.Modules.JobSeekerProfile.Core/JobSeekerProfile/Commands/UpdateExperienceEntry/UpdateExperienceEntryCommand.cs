using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UpdateExperienceEntry;

public record UpdateExperienceEntryCommand(
    Guid UserId,
    Guid ExperienceEntryId,
    string Company,
    string Role,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsCurrent,
    string Responsibilities) : ICommand;
