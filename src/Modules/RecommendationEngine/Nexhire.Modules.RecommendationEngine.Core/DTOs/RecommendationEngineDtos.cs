using System;
using System.Collections.Generic;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Core.DTOs;

public sealed record RecommendedJobDto(
    Guid JobPostingId,
    int MatchScore,
    decimal HybridScore,
    string Summary,
    List<MatchFactor> TopFactors,
    bool IsSuppressed);

public sealed record FactorScoreDto(
    string Factor,
    int Score);

public sealed record MatchDetailsDto(
    Guid JobSeekerId,
    Guid JobPostingId,
    int OverallScore,
    string WeightProfileVersion,
    string WeightVariantId,
    List<FactorScoreDto> Breakdown,
    List<MatchFactor> Strengths,
    List<MatchFactor> Gaps,
    bool IsStale);

public sealed record ShortlistCandidateDto(
    Guid JobSeekerId,
    int OverallMatchScore,
    string InclusionReason,
    int OverallScore,
    List<string> Strengths,
    List<string> Gaps,
    string SalaryFit,
    int SalaryMatchPercent,
    int MotivationScore,
    string TimeToProductivity,
    string ContactLikelihood,
    bool WorkArrangementCompatible,
    DateTime? AppliedAtUtc);

public sealed record CandidateShortlistDto(
    Guid JobPostingId,
    string RefreshState,
    DateTime LastRefreshedUtc,
    List<ShortlistCandidateDto> Candidates);

public sealed record CandidateSearchResultDto(
    Guid JobSeekerId,
    string SeekerName,
    int MatchScore,
    List<string> Skills,
    string EducationLevel,
    decimal TotalExperienceYears,
    string City,
    decimal ExpectedSalaryMin,
    decimal ExpectedSalaryMax,
    string Currency);

public sealed record ThresholdImpactPreviewDto(
    int TotalSamplesAnalyzed,
    int MatchesAboveCurrentThreshold,
    int MatchesAboveProposedThreshold,
    int CandidatesExcludedCount,
    int CandidatesNewlyIncludedCount,
    decimal ShortlistPercentChange);

public sealed record MatchingWeightProfileDto(
    Guid ProfileId,
    string Version,
    decimal Skill,
    decimal Education,
    decimal Training,
    decimal Location,
    decimal Experience,
    decimal Salary,
    string VariantId,
    int VariantAllocationPercent,
    bool IsActive,
    string? SupersededByVersion);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    long TotalCount)
{
    public bool NoResults => TotalCount == 0;
}

public sealed record FitAnalysisDto(
    Guid JobSeekerId,
    Guid JobPostingId,
    int MatchScore,
    List<string> Strengths,
    List<string> Gaps,
    string SalaryFit,
    int SalaryMatchPercent,
    int MotivationScore,
    string TimeToProductivity,
    string ContactLikelihood,
    bool WorkArrangementCompatible);

public sealed record ThresholdConfigDto(
    int GlobalThresholdPercent,
    DateTime UpdatedOnUtc,
    List<PerPostingThresholdOverrideDto> PerPostingOverrides);

public sealed record PerPostingThresholdOverrideDto(
    Guid JobPostingId,
    int ThresholdPercent);

public sealed record WeightProfileVersionDto(
    Guid ProfileId,
    string Version,
    string VariantId,
    bool IsActive,
    DateTime CreatedOnUtc,
    string? SupersededByVersion);

public sealed record TalentPoolSummaryDto(
    Guid PoolId,
    string Name,
    string? Description,
    int ActiveCandidateCount,
    bool IsShared,
    DateTime CreatedOnUtc);

public sealed record TalentPoolDetailDto(
    Guid PoolId,
    string Name,
    string? Description,
    List<string> AssociatedSkills,
    bool IsShared,
    List<TalentPoolCandidateDto> Candidates,
    DateTime CreatedOnUtc,
    DateTime UpdatedOnUtc);

public sealed record TalentPoolCandidateDto(
    Guid JobSeekerId,
    string? Note,
    bool IsActive,
    DateTime AddedOnUtc);

