using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.RemoveEmployerDocument;

public record RemoveEmployerDocumentCommand(Guid UserId, Guid DocumentId) : ICommand;
