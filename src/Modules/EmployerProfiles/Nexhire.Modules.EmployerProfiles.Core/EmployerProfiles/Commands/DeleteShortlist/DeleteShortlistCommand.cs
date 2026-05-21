using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.DeleteShortlist;

public record DeleteShortlistCommand(Guid UserId, Guid ShortlistId) : ICommand;
