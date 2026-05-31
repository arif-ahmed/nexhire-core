namespace Nexhire.Modules.IdentityAccess.Application.DTOs;

public record AccountDto(
    Guid UserId, 
    string Email, 
    string? MobileMasked, 
    string Role, 
    string Status, 
    bool MfaEnabled, 
    bool IdentityVerified);
