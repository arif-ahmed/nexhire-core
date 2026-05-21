namespace Nexhire.Modules.EmployerProfiles.Core.DTOs;

public record ShortlistDto(
    Guid Id,
    string Name,
    int MemberCount,
    DateTime CreatedOnUtc,
    DateTime UpdatedOnUtc);
