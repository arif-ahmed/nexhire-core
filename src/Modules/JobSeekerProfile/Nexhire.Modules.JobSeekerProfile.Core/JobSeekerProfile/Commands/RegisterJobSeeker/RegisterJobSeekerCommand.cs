using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RegisterJobSeeker;

public record RegisterJobSeekerCommand(
    string Email,
    string Mobile,
    string Password,
    string FirstName,
    string LastName,
    string Gender) : ICommand<Guid>;
