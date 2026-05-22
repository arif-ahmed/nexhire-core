using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddEducationEntry;

public record AddEducationEntryCommand(
    Guid UserId,
    string Degree,
    string Institution,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? Gpa) : ICommand;
