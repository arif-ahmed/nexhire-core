namespace Nexhire.Modules.EmployerProfiles.Application.DTOs;

public record MatchedCandidateDto(
    Guid Id,
    Guid PostingId,
    Guid CandidateUserId,
    int MatchScore,
    DateTime GeneratedOnUtc);
