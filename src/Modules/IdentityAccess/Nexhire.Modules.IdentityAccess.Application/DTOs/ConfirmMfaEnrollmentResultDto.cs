namespace Nexhire.Modules.IdentityAccess.Application.DTOs;

public record ConfirmMfaEnrollmentResultDto(IReadOnlyList<string> BackupCodes);
