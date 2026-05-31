using Nexhire.Modules.EmployerProfiles.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.UploadEmployerDocument;

public record UploadEmployerDocumentCommand(Guid UserId, byte[] Content, string FileName, string MimeType, DocumentKind Kind) : ICommand<Guid>;
