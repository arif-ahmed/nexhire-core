using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.UploadEmployerLogo;

public record UploadEmployerLogoCommand(Guid UserId, byte[] Content, string FileName, string MimeType) : ICommand;
