using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UploadSupplementaryDocument;

public record UploadSupplementaryDocumentCommand(
    Guid UserId,
    byte[] Content,
    string FileName,
    string MimeType,
    string Kind) : ICommand;
