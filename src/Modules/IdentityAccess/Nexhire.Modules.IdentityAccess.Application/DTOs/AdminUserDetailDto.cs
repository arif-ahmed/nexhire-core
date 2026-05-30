namespace Nexhire.Modules.IdentityAccess.Application.DTOs;

public record AdminUserDetailDto(
    Guid UserId, 
    string Email, 
    string Mobile, 
    string Role, 
    string Status, 
    bool IdentityVerified, 
    bool IsLocked, 
    DateTime? LockedUntilUtc, 
    int FailedLoginCount, 
    int FailedOtpCount, 
    int SessionCount);
