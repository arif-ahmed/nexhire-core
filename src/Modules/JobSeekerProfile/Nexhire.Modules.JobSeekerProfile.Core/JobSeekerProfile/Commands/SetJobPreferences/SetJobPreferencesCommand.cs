using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetJobPreferences;

public record SetJobPreferencesCommand(
    Guid UserId,
    IEnumerable<string> JobTypes,
    IEnumerable<string> Industries,
    IEnumerable<string> Locations,
    IEnumerable<string> WorkArrangements,
    decimal? MinSalaryExpectation = null,
    decimal? MaxSalaryExpectation = null,
    string? SalaryCurrency = null) : ICommand;
