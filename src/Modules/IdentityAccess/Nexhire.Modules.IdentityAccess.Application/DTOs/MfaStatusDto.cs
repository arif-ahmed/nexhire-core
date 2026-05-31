namespace Nexhire.Modules.IdentityAccess.Application.DTOs;

public record MfaStatusDto(
    bool Enabled, 
    string? Method, 
    DateTime? LastVerifiedUtc, 
    int BackupCodesRemaining);
