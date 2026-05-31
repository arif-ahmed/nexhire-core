namespace Nexhire.Modules.IdentityAccess.Application.DTOs;

public record UserListItemDto(
    Guid UserId, 
    string Email, 
    string Role, 
    string Status, 
    DateTime CreatedOnUtc, 
    bool IdentityVerified);
