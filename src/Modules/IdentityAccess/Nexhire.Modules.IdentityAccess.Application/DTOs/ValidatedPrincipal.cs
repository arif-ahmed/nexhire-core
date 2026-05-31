namespace Nexhire.Modules.IdentityAccess.Application.DTOs;

public record ValidatedPrincipal(
    Guid UserId, 
    string Role, 
    IReadOnlyList<string> Permissions, 
    Guid SessionId);
