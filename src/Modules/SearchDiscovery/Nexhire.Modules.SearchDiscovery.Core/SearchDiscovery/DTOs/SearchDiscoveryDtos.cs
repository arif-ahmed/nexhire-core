namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;

public record SearchResultDto(
    IReadOnlyList<RankedJobDto> Items,
    IReadOnlyList<RankedJobDto> Recommendations,
    int Page,
    int PageSize,
    int TotalCount,
    string AppliedSort,
    bool NoResults);

public record RankedJobDto(
    Guid PostingId,
    string Title,
    string Summary,
    string CompanyName,
    string[] Skills,
    string? EmploymentType,
    string? WorkFormat,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? LocationDistrict,
    string? LocationCity,
    DateTime PostedOnUtc,
    DateTime? ApplicationDeadlineUtc,
    double RelevanceScore,
    int? MatchScore,
    bool IsRecommended);

public record FavoriteJobDto(
    Guid FavoriteJobId,
    Guid PostingId,
    string? Title,
    string? CompanyName,
    DateTime FavoritedOnUtc,
    bool NoLongerAvailable);

public record SavedSearchDto(
    Guid SavedSearchId,
    string Name,
    string? CriteriaSummary,
    string NotificationPreference,
    DateTime? LastEvaluatedOnUtc,
    DateTime CreatedOnUtc);

public record SearchSessionDto(
    Guid SessionId,
    Guid SeekerUserId,
    object? LastCriteria,
    IReadOnlyList<Guid> DismissedRecommendationPostingIds,
    DateTime ExpiresOnUtc);
