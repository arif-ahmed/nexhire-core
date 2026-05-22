using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UpdateEducationEntry;

public record UpdateEducationEntryCommand(
    Guid UserId,
    Guid EducationEntryId,
    string Degree,
    string Institution,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? Gpa) : ICommand;
