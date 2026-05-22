using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetProfileVisibility;

public record SetProfileVisibilityCommand(Guid UserId, string Visibility) : ICommand;
