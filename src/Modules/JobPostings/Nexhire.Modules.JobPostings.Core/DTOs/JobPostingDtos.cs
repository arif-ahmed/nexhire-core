using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobPostings.Core.DTOs;

public sealed record SkillInput(string RawLabelOrCode, SkillImportance Importance = SkillImportance.Mandatory);
public sealed record LanguageRequirementDto(string Language, string Proficiency);
public sealed record EmploymentLocationDto(string Line1, string City, string District, string Country);
public sealed record SalaryRangeDto(decimal Min, decimal Max, string? Currency, SalaryPeriod Period);
public sealed record TargetingCriteriaDto(IReadOnlyCollection<string> SkillCodes, IReadOnlyCollection<string> Locations, IReadOnlyCollection<Guid> SeekerGroupIds);
public sealed record PostingVisibilityDto(VisibilityLevel Level, TargetingCriteriaDto? TargetingCriteria);

public sealed record JobPostingDraftDto(
    string Title,
    string Summary,
    ContractType ContractType,
    EducationLevel EducationLevel,
    WorkFormat WorkFormat,
    EmploymentLocationDto? Location,
    IReadOnlyCollection<SkillInput> RequiredSkills,
    IReadOnlyCollection<LanguageRequirementDto> RequiredLanguages,
    DateTime DeadlineUtc,
    bool AutoCloseEnabled,
    string? JobLink,
    SalaryRangeDto? SalaryRange,
    PostingVisibilityDto Visibility);

public sealed record JobPostingSummaryDto(Guid Id, string Title, string Status, DateTime DeadlineUtc, string Visibility, DateTime UpdatedOnUtc);
public sealed record JobPostingDto(Guid Id, Guid EmployerId, string Title, string Summary, string Status, string ContractType, string EducationLevel, string WorkFormat, DateTime DeadlineUtc, string Visibility, IReadOnlyCollection<string> RequiredSkillCodes, DateTime CreatedOnUtc, DateTime UpdatedOnUtc, DateTime? PublishedOnUtc, Guid? RenewedFromPostingId);
public sealed record SchemaOrgJobPostingDto(IReadOnlyDictionary<string, string> Properties, bool IsCompliant, IReadOnlyCollection<string> Violations);
public sealed record AuditEntryDto(string Kind, string ActorKind, Guid? ActorUserId, string ActorDisplayName, string? FromStatus, string? ToStatus, IReadOnlyCollection<string> ChangedFields, string? Reason, DateTime OccurredOnUtc);
public sealed record AdminJobPostingDetailDto(JobPostingDto Posting, int ApplicationsCount, int MatchesCount, int ViewsCount);
public sealed record AdminJobPostingListItemDto(Guid Id, Guid EmployerId, string Title, string Status, string Location, DateTime DeadlineUtc, DateTime CreatedOnUtc, DateTime UpdatedOnUtc);
public sealed record BulkRenewResultDto(Guid JobPostingId, bool Success, Guid? NewJobPostingId, string? ErrorCode, string? Message);
