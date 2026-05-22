using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddExperienceEntry;

public record AddExperienceEntryCommand(
    Guid UserId,
    string Company,
    string Role,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsCurrent,
    string Responsibilities) : ICommand;
