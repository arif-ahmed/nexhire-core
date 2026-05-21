using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UploadCompanyImage;

public record UploadCompanyImageCommand(Guid UserId, byte[] Content, string FileName, string MimeType) : ICommand;
