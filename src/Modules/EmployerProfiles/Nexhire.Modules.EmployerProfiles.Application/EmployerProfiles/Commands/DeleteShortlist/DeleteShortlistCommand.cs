using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.DeleteShortlist;

public record DeleteShortlistCommand(Guid UserId, Guid ShortlistId) : ICommand;
