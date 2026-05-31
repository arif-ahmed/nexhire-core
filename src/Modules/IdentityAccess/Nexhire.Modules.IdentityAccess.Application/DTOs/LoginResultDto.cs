namespace Nexhire.Modules.IdentityAccess.Application.DTOs;

public record LoginResultDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresIn,
    bool RequiresMfa,
    Guid? MfaChallengeId);
