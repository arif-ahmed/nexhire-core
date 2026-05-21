using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UploadEmployerDocument;

public record UploadEmployerDocumentCommand(Guid UserId, byte[] Content, string FileName, string MimeType, DocumentKind Kind) : ICommand;
