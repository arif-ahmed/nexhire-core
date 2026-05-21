namespace Nexhire.Modules.EmployerProfiles.Core.DTOs;

public record ShortlistDetailDto(
    Guid Id,
    string Name,
    IReadOnlyCollection<ShortlistMemberDto> Members,
    DateTime CreatedOnUtc,
    DateTime UpdatedOnUtc);

public record ShortlistMemberDto(
    Guid Id,
    Guid CandidateUserId,
    int? MatchScore,
    DateTime AddedOnUtc);
