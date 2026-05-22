using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetRecentSalary;

public record SetRecentSalaryCommand(
    Guid UserId,
    decimal? Amount,
    string? Currency) : ICommand;
