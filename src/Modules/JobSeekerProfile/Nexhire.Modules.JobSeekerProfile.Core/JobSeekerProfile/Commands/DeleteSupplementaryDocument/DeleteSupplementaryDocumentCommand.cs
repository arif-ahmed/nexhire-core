using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.DeleteSupplementaryDocument;

public record DeleteSupplementaryDocumentCommand(Guid UserId, Guid DocumentId) : ICommand;
