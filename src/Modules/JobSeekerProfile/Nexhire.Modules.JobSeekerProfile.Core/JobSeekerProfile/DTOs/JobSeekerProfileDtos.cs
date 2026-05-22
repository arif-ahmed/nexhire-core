using System;
using System.Collections.Generic;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.DTOs;

public record JobSeekerProfileDto(
    Guid Id,
    Guid UserId,
    string Status,
    string FirstName,
    string LastName,
    string Email,
    string Mobile,
    string Gender,
    IReadOnlyCollection<EducationEntryDto> Education,
    IReadOnlyCollection<ExperienceEntryDto> Experience,
    IReadOnlyCollection<ProfileSkillDto> Skills,
    IReadOnlyCollection<SupplementaryDocumentDto> Documents,
    JobPreferencesDto? Preferences,
    AddressDto? CurrentAddress,
    AddressDto? PermanentAddress,
    MoneyDto? RecentSalary,
    string Visibility,
    PublicSharingSettingsDto PublicSharing,
    VerificationFlagsDto Verification,
    bool HasActiveResume,
    CompletenessScoreDto Completeness);

public record EducationEntryDto(
    Guid Id,
    string Degree,
    string Institution,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? Gpa);

public record ExperienceEntryDto(
    Guid Id,
    string Company,
    string Role,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsCurrent,
    string Responsibilities);

public record ProfileSkillDto(
    Guid Id,
    string TaxonomyCode,
    string DisplayLabel,
    string RawLabel,
    string Category,
    string Tier,
    int Proficiency);

public record SupplementaryDocumentDto(
    Guid Id,
    string StorageKey,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    string Kind,
    string ScanStatus,
    DateTime UploadedOnUtc);

public record JobPreferencesDto(
    IReadOnlyCollection<string> JobTypes,
    IReadOnlyCollection<string> Industries,
    IReadOnlyCollection<string> Locations,
    IReadOnlyCollection<string> WorkArrangements,
    SalaryExpectationDto? SalaryExpectation);

public record SalaryExpectationDto(
    MoneyDto Min,
    MoneyDto Max);

public record MoneyDto(
    decimal Amount,
    string Currency);

public record AddressDto(
    string Line1,
    string? Line2,
    string City,
    string District,
    string Postcode,
    string Country);

public record PublicSharingSettingsDto(
    bool Enabled,
    string? Slug,
    string? QrCodeStorageKey);

public record VerificationFlagsDto(
    bool IdentityVerified,
    bool EducationVerified,
    bool SelfAttested);

public record CompletenessScoreDto(
    int Percentage,
    IReadOnlyCollection<string> MissingSections);

public record ProfileHistoryDto(
    Guid Id,
    Guid JobSeekerProfileId,
    IReadOnlyCollection<ProfileVersionDto> Versions);

public record ProfileVersionDto(
    Guid Id,
    string Action,
    IReadOnlyCollection<string> ChangedFields,
    DateTime CreatedOnUtc);

public record ResumeParseStatusDto(
    Guid ResumeId,
    string ParseStatus,
    string? FailureReason,
    ParsedResumeDataDto? ParsedData,
    DateTime? ParsedOnUtc);

public record ParsedResumeDataDto(
    string? FullName,
    string? Email,
    string? Phone,
    IReadOnlyCollection<ParsedEducationDto> Education,
    IReadOnlyCollection<ParsedExperienceDto> Experience,
    IReadOnlyCollection<ParsedSkillDto> Skills);

public record ParsedEducationDto(
    string Degree,
    string Institution,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal? Gpa);

public record ParsedExperienceDto(
    string Company,
    string Role,
    DateTime? StartDate,
    DateTime? EndDate,
    bool IsCurrent,
    string? Responsibilities);

public record ParsedSkillDto(
    string RawLabel,
    double ConfidenceScore,
    bool IsLowConfidence);
