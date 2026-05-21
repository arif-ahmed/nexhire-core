using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.CreateShortlist;

public record CreateShortlistCommand(Guid UserId, string Name) : ICommand<Guid>;
