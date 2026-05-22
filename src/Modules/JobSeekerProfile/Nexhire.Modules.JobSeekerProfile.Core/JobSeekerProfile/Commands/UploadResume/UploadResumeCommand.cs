using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UploadResume;

public record UploadResumeCommand(
    Guid UserId,
    byte[] Content,
    string FileName,
    string MimeType) : ICommand;
