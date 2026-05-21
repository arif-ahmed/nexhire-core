namespace Nexhire.Modules.EmployerProfiles.Core.DTOs;

public record MatchedCandidateDto(
    Guid Id,
    Guid PostingId,
    Guid CandidateUserId,
    int MatchScore,
    DateTime GeneratedOnUtc);
