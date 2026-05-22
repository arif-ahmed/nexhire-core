using System;
using System.Collections.Generic;
using Nexhire.Modules.RecommendationEngine.Core.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.RecommendationEngine.Core.RecommendationEngine.Queries;

public sealed record GetJobRecommendationsQuery(Guid JobSeekerId, int Limit = 10) : IQuery<IReadOnlyCollection<RecommendedJobDto>>;

public sealed record GetMatchDetailsQuery(Guid JobSeekerId, Guid JobPostingId) : IQuery<MatchDetailsDto>;

public sealed record GetCandidateShortlistQuery(Guid JobPostingId, Guid RecruiterId) : IQuery<CandidateShortlistDto>;

public sealed record SearchCandidatesQuery(
    string? Keyword = null,
    string? Skills = null,
    string? EducationLevel = null,
    decimal? MinExperience = null,
    decimal? MaxSalary = null,
    int Page = 1,
    int PageSize = 10) : IQuery<PagedResult<CandidateSearchResultDto>>;

public sealed record PreviewThresholdImpactQuery(
    Guid JobPostingId,
    int ProposedThreshold) : IQuery<ThresholdImpactPreviewDto>;

public sealed record GetMatchingWeightsQuery() : IQuery<IReadOnlyCollection<MatchingWeightProfileDto>>;

public sealed record GetCandidateFitAnalysisQuery(Guid PostingId, Guid SeekerId) : IQuery<FitAnalysisDto>;

public sealed record GetMatchThresholdConfigQuery() : IQuery<ThresholdConfigDto>;

public sealed record GetWeightProfileHistoryQuery(int Take = 10) : IQuery<IReadOnlyCollection<WeightProfileVersionDto>>;

public sealed record GetTalentPoolsQuery(Guid RecruiterId, Guid EmployerId) : IQuery<IReadOnlyCollection<TalentPoolSummaryDto>>;

public sealed record GetTalentPoolQuery(Guid PoolId) : IQuery<TalentPoolDetailDto>;
