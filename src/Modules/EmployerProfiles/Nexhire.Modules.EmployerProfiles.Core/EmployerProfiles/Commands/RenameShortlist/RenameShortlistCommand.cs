using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RenameShortlist;

public record RenameShortlistCommand(Guid UserId, Guid ShortlistId, string NewName) : ICommand;
