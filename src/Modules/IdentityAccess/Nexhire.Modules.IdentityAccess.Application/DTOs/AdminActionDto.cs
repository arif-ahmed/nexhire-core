namespace Nexhire.Modules.IdentityAccess.Application.DTOs;

public record AdminActionDto(
    Guid Id, 
    Guid AdminUserId, 
    string ActionType, 
    Guid TargetUserId, 
    string? Reason, 
    DateTime OccurredOnUtc);
