namespace Nexhire.Modules.IdentityAccess.Application.DTOs;

public record SessionDto(
    Guid SessionId, 
    string Channel, 
    string? DeviceLabel, 
    DateTime IssuedOnUtc, 
    DateTime LastSeenUtc, 
    bool IsCurrent);
