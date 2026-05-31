namespace Nexhire.Modules.EmployerProfiles.Application.DTOs;

public record ShortlistDto(
    Guid Id,
    string Name,
    int MemberCount,
    DateTime CreatedOnUtc,
    DateTime UpdatedOnUtc);
