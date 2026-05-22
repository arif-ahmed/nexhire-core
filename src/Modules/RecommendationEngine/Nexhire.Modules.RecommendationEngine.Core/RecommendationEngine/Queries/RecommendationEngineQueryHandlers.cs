using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Ports;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Services;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Modules.RecommendationEngine.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.RecommendationEngine.Core.RecommendationEngine.Queries;

public sealed class GetJobRecommendationsQueryHandler : IQueryHandler<GetJobRecommendationsQuery, IReadOnlyCollection<RecommendedJobDto>>
{
    private readonly IJobRecommendationSetRepository _recommendationSetRepository;

    public GetJobRecommendationsQueryHandler(IJobRecommendationSetRepository recommendationSetRepository)
    {
        _recommendationSetRepository = recommendationSetRepository;
    }

    public async Task<Result<IReadOnlyCollection<RecommendedJobDto>>> Handle(GetJobRecommendationsQuery request, CancellationToken cancellationToken)
    {
        var set = await _recommendationSetRepository.GetBySeekerIdAsync(request.JobSeekerId, cancellationToken);
        if (set == null)
        {
            return Result.Success<IReadOnlyCollection<RecommendedJobDto>>(Array.Empty<RecommendedJobDto>());
        }

        var list = set.Recommendations
            .Where(r => !r.IsSuppressed)
            .Take(request.Limit)
            .Select(r => new RecommendedJobDto(
                r.JobPostingId,
                r.MatchScore,
                r.HybridScore,
                r.Reason.Summary,
                r.Reason.TopFactors.ToList(),
                r.IsSuppressed))
            .ToList();

        return Result.Success<IReadOnlyCollection<RecommendedJobDto>>(list);
    }
}

public sealed class GetMatchDetailsQueryHandler : IQueryHandler<GetMatchDetailsQuery, MatchDetailsDto>
{
    private readonly IMatchScoreRepository _matchScoreRepository;

    public GetMatchDetailsQueryHandler(IMatchScoreRepository matchScoreRepository)
    {
        _matchScoreRepository = matchScoreRepository;
    }

    public async Task<Result<MatchDetailsDto>> Handle(GetMatchDetailsQuery request, CancellationToken cancellationToken)
    {
        var score = await _matchScoreRepository.GetByKeysAsync(request.JobSeekerId, request.JobPostingId, cancellationToken);
        if (score == null)
        {
            return Result.Failure<MatchDetailsDto>(new Error("E-MATCH-SCORE-NOT-FOUND", "Match score not found."));
        }

        var breakdownList = score.Breakdown.Scores
            .Select(s => new FactorScoreDto(s.Factor.ToString(), s.Score))
            .ToList();

        return Result.Success(new MatchDetailsDto(
            score.JobSeekerId,
            score.JobPostingId,
            score.OverallScore,
            score.WeightProfileVersion,
            score.WeightVariantId,
            breakdownList,
            score.Strengths,
            score.Gaps,
            score.IsStale));
    }
}

public sealed class GetCandidateShortlistQueryHandler : IQueryHandler<GetCandidateShortlistQuery, CandidateShortlistDto>
{
    private readonly ICandidateShortlistRepository _shortlistRepository;

    public GetCandidateShortlistQueryHandler(ICandidateShortlistRepository shortlistRepository)
    {
        _shortlistRepository = shortlistRepository;
    }

    public async Task<Result<CandidateShortlistDto>> Handle(GetCandidateShortlistQuery request, CancellationToken cancellationToken)
    {
        var shortlist = await _shortlistRepository.GetByPostingIdAsync(request.JobPostingId, cancellationToken);
        if (shortlist == null)
        {
            return Result.Success(new CandidateShortlistDto(
                request.JobPostingId,
                ShortlistRefreshState.Fresh.ToString(),
                DateTime.UtcNow,
                new List<ShortlistCandidateDto>()));
        }

        var candidatesDto = shortlist.Candidates.Select(c => new ShortlistCandidateDto(
            c.JobSeekerId,
            c.OverallMatchScore,
            c.InclusionReason.ToString(),
            c.FitAnalysis.OverallScore,
            c.FitAnalysis.Strengths.Select(s => s.ToString()).ToList(),
            c.FitAnalysis.Gaps.Select(g => g.ToString()).ToList(),
            c.FitAnalysis.SalaryFit.ToString(),
            c.FitAnalysis.SalaryMatchPercent,
            c.FitAnalysis.MotivationScore,
            c.FitAnalysis.TimeToProductivity.ToString(),
            c.FitAnalysis.ContactLikelihood.ToString(),
            c.FitAnalysis.WorkArrangementCompatible,
            c.AppliedAtUtc)).ToList();

        return Result.Success(new CandidateShortlistDto(
            shortlist.JobPostingId,
            shortlist.RefreshState.ToString(),
            shortlist.LastRefreshedUtc,
            candidatesDto));
    }
}

public sealed class SearchCandidatesQueryHandler : IQueryHandler<SearchCandidatesQuery, PagedResult<CandidateSearchResultDto>>
{
    private readonly ISeekerMatchProfileRepository _seekerRepository;

    public SearchCandidatesQueryHandler(ISeekerMatchProfileRepository seekerRepository)
    {
        _seekerRepository = seekerRepository;
    }

    public async Task<Result<PagedResult<CandidateSearchResultDto>>> Handle(SearchCandidatesQuery request, CancellationToken cancellationToken)
    {
        var profiles = await _seekerRepository.GetActiveProfilesAsync(cancellationToken);
        var query = profiles.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.ToLowerInvariant();
            query = query.Where(p => 
                p.Skills.Any(s => s.DisplayLabel.ToLowerInvariant().Contains(kw)) ||
                (p.Location?.City.ToLowerInvariant().Contains(kw) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(request.Skills))
        {
            var skillsList = request.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant()).ToList();
            query = query.Where(p => 
                p.Skills.Any(s => skillsList.Any(sk => s.DisplayLabel.ToLowerInvariant().Contains(sk) || s.TaxonomyCode.ToLowerInvariant().Contains(sk))));
        }

        if (!string.IsNullOrWhiteSpace(request.EducationLevel) && Enum.TryParse<EducationLevel>(request.EducationLevel, true, out var minEdu))
        {
            query = query.Where(p => p.EducationLevel >= minEdu);
        }

        if (request.MinExperience.HasValue)
        {
            query = query.Where(p => p.TotalExperienceYears >= request.MinExperience.Value);
        }

        if (request.MaxSalary.HasValue)
        {
            query = query.Where(p => p.SalaryExpectation == null || p.SalaryExpectation.Min <= request.MaxSalary.Value);
        }

        var allMatching = query.ToList();
        var totalCount = allMatching.Count;
        var paged = allMatching
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new CandidateSearchResultDto(
                p.JobSeekerId,
                $"Candidate {p.JobSeekerId.ToString().Substring(0, 8).ToUpper()}",
                85, // Default overall score representation
                p.Skills.Select(s => s.DisplayLabel).ToList(),
                p.EducationLevel.ToString(),
                p.TotalExperienceYears,
                p.Location?.City ?? "N/A",
                p.SalaryExpectation?.Min ?? 0,
                p.SalaryExpectation?.Max ?? 0,
                p.SalaryExpectation?.Currency ?? "USD"))
            .ToList();

        return Result.Success(new PagedResult<CandidateSearchResultDto>(
            paged,
            request.Page,
            request.PageSize,
            totalCount));
    }
}

public sealed class PreviewThresholdImpactQueryHandler : IQueryHandler<PreviewThresholdImpactQuery, ThresholdImpactPreviewDto>
{
    private readonly IPostingMatchProfileRepository _postingRepository;
    private readonly IMatchThresholdConfigurationRepository _configRepository;
    private readonly IMatchScoreRepository _matchScoreRepository;
    private readonly ImpactPreviewCalculator _impactCalculator;

    public PreviewThresholdImpactQueryHandler(
        IPostingMatchProfileRepository postingRepository,
        IMatchThresholdConfigurationRepository configRepository,
        IMatchScoreRepository matchScoreRepository,
        ImpactPreviewCalculator impactCalculator)
    {
        _postingRepository = postingRepository;
        _configRepository = configRepository;
        _matchScoreRepository = matchScoreRepository;
        _impactCalculator = impactCalculator;
    }

    public async Task<Result<ThresholdImpactPreviewDto>> Handle(PreviewThresholdImpactQuery request, CancellationToken cancellationToken)
    {
        var posting = await _postingRepository.GetByPostingIdAsync(request.JobPostingId, cancellationToken);
        int currentThreshold = 60;
        if (posting?.PerPostingThresholdOverride != null)
        {
            currentThreshold = posting.PerPostingThresholdOverride.Value;
        }
        else
        {
            var defaultConfig = await _configRepository.GetDefaultAsync(cancellationToken);
            if (defaultConfig != null)
            {
                currentThreshold = defaultConfig.GlobalThresholdPercent;
            }
        }

        var sampleMatchScores = await _matchScoreRepository.GetByPostingIdAsync(request.JobPostingId, cancellationToken);
        var preview = _impactCalculator.PreviewImpact(currentThreshold, request.ProposedThreshold, sampleMatchScores);

        return Result.Success(new ThresholdImpactPreviewDto(
            preview.TotalSamplesAnalyzed,
            preview.MatchesAboveCurrentThreshold,
            preview.MatchesAboveProposedThreshold,
            preview.CandidatesExcludedCount,
            preview.CandidatesNewlyIncludedCount,
            preview.ShortlistPercentChange));
    }
}

public sealed class GetMatchingWeightsQueryHandler : IQueryHandler<GetMatchingWeightsQuery, IReadOnlyCollection<MatchingWeightProfileDto>>
{
    private readonly IMatchingWeightProfileRepository _weightRepository;

    public GetMatchingWeightsQueryHandler(IMatchingWeightProfileRepository weightRepository)
    {
        _weightRepository = weightRepository;
    }

    public async Task<Result<IReadOnlyCollection<MatchingWeightProfileDto>>> Handle(GetMatchingWeightsQuery request, CancellationToken cancellationToken)
    {
        var profiles = await _weightRepository.GetActiveProfilesAsync(cancellationToken);
        if (profiles.Count == 0)
        {
            var initial = MatchingWeightProfile.CreateInitial();
            profiles = new List<MatchingWeightProfile> { initial };
        }

        var dtos = profiles.Select(p => new MatchingWeightProfileDto(
            p.Id.Value,
            p.Version,
            p.Weights.Skill,
            p.Weights.Education,
            p.Weights.Training,
            p.Weights.Location,
            p.Weights.Experience,
            p.Weights.Salary,
            p.VariantId,
            p.VariantAllocationPercent,
            p.IsActive,
            p.SupersededByVersion)).ToList();

        return Result.Success<IReadOnlyCollection<MatchingWeightProfileDto>>(dtos);
    }
}

public sealed class GetCandidateFitAnalysisQueryHandler : IQueryHandler<GetCandidateFitAnalysisQuery, FitAnalysisDto>
{
    private readonly IMatchScoreRepository _matchScoreRepository;
    private readonly ISeekerMatchProfileRepository _seekerRepository;
    private readonly IPostingMatchProfileRepository _postingRepository;
    private readonly FitAnalysisService _fitAnalysisService;

    public GetCandidateFitAnalysisQueryHandler(
        IMatchScoreRepository matchScoreRepository,
        ISeekerMatchProfileRepository seekerRepository,
        IPostingMatchProfileRepository postingRepository,
        FitAnalysisService fitAnalysisService)
    {
        _matchScoreRepository = matchScoreRepository;
        _seekerRepository = seekerRepository;
        _postingRepository = postingRepository;
        _fitAnalysisService = fitAnalysisService;
    }

    public async Task<Result<FitAnalysisDto>> Handle(GetCandidateFitAnalysisQuery request, CancellationToken cancellationToken)
    {
        var score = await _matchScoreRepository.GetByKeysAsync(request.SeekerId, request.PostingId, cancellationToken);
        if (score == null)
        {
            return Result.Failure<FitAnalysisDto>(new Error("E-MATCH-SCORE-NOT-FOUND", "Match score not found for this candidate-posting pair."));
        }

        var seeker = await _seekerRepository.GetBySeekerIdAsync(request.SeekerId, cancellationToken);
        var posting = await _postingRepository.GetByPostingIdAsync(request.PostingId, cancellationToken);
        if (seeker == null || posting == null)
        {
            return Result.Failure<FitAnalysisDto>(new Error("E-PROFILE-NOT-FOUND", "Seeker or posting profile not found."));
        }

        var fit = _fitAnalysisService.AnalyzeFit(seeker, posting, score);

        return Result.Success(new FitAnalysisDto(
            request.SeekerId,
            request.PostingId,
            fit.OverallScore,
            fit.Strengths.Select(s => s.ToString()).ToList(),
            fit.Gaps.Select(g => g.ToString()).ToList(),
            fit.SalaryFit.ToString(),
            fit.SalaryMatchPercent,
            fit.MotivationScore,
            fit.TimeToProductivity.ToString(),
            fit.ContactLikelihood.ToString(),
            fit.WorkArrangementCompatible));
    }
}

public sealed class GetMatchThresholdConfigQueryHandler : IQueryHandler<GetMatchThresholdConfigQuery, ThresholdConfigDto>
{
    private readonly IMatchThresholdConfigurationRepository _configRepository;
    private readonly IPostingMatchProfileRepository _postingRepository;

    public GetMatchThresholdConfigQueryHandler(
        IMatchThresholdConfigurationRepository configRepository,
        IPostingMatchProfileRepository postingRepository)
    {
        _configRepository = configRepository;
        _postingRepository = postingRepository;
    }

    public async Task<Result<ThresholdConfigDto>> Handle(GetMatchThresholdConfigQuery request, CancellationToken cancellationToken)
    {
        var config = await _configRepository.GetDefaultAsync(cancellationToken);
        if (config == null)
        {
            config = MatchThresholdConfiguration.CreateDefault();
        }

        var postings = await _postingRepository.GetActivePostingsAsync(cancellationToken);
        var overrides = postings
            .Where(p => p.PerPostingThresholdOverride.HasValue)
            .Select(p => new PerPostingThresholdOverrideDto(p.JobPostingId, p.PerPostingThresholdOverride!.Value))
            .ToList();

        return Result.Success(new ThresholdConfigDto(
            config.GlobalThresholdPercent,
            DateTime.UtcNow,
            overrides));
    }
}

public sealed class GetWeightProfileHistoryQueryHandler : IQueryHandler<GetWeightProfileHistoryQuery, IReadOnlyCollection<WeightProfileVersionDto>>
{
    private readonly IMatchingWeightProfileRepository _weightRepository;

    public GetWeightProfileHistoryQueryHandler(IMatchingWeightProfileRepository weightRepository)
    {
        _weightRepository = weightRepository;
    }

    public async Task<Result<IReadOnlyCollection<WeightProfileVersionDto>>> Handle(GetWeightProfileHistoryQuery request, CancellationToken cancellationToken)
    {
        var profiles = await _weightRepository.GetHistoricalVersionsAsync("control", request.Take, cancellationToken);
        var dtos = profiles.Select(p => new WeightProfileVersionDto(
            p.Id.Value,
            p.Version,
            p.VariantId,
            p.IsActive,
            DateTime.UtcNow,
            p.SupersededByVersion)).ToList();

        return Result.Success<IReadOnlyCollection<WeightProfileVersionDto>>(dtos);
    }
}

public sealed class GetTalentPoolsQueryHandler : IQueryHandler<GetTalentPoolsQuery, IReadOnlyCollection<TalentPoolSummaryDto>>
{
    private readonly ITalentPoolRepository _poolRepository;

    public GetTalentPoolsQueryHandler(ITalentPoolRepository poolRepository)
    {
        _poolRepository = poolRepository;
    }

    public async Task<Result<IReadOnlyCollection<TalentPoolSummaryDto>>> Handle(GetTalentPoolsQuery request, CancellationToken cancellationToken)
    {
        var pools = await _poolRepository.GetByEmployerIdAsync(request.EmployerId, cancellationToken);
        var dtos = pools.Select(p => new TalentPoolSummaryDto(
            p.Id.Value,
            p.Name,
            p.Description,
            p.Members.Count(m => m.IsActive),
            p.IsShared,
            DateTime.UtcNow)).ToList();

        return Result.Success<IReadOnlyCollection<TalentPoolSummaryDto>>(dtos);
    }
}

public sealed class GetTalentPoolQueryHandler : IQueryHandler<GetTalentPoolQuery, TalentPoolDetailDto>
{
    private readonly ITalentPoolRepository _poolRepository;

    public GetTalentPoolQueryHandler(ITalentPoolRepository poolRepository)
    {
        _poolRepository = poolRepository;
    }

    public async Task<Result<TalentPoolDetailDto>> Handle(GetTalentPoolQuery request, CancellationToken cancellationToken)
    {
        var pool = await _poolRepository.GetByIdAsync(new TalentPoolId(request.PoolId), cancellationToken);
        if (pool == null)
        {
            return Result.Failure<TalentPoolDetailDto>(new Error("E-POOL-NOT-FOUND", "Talent pool not found."));
        }

        var candidates = pool.Members.Select(m => new TalentPoolCandidateDto(
            m.JobSeekerId,
            m.Note,
            m.IsActive,
            m.AddedAtUtc)).ToList();

        return Result.Success(new TalentPoolDetailDto(
            pool.Id.Value,
            pool.Name,
            pool.Description,
            pool.Tags,
            pool.IsShared,
            candidates,
            DateTime.UtcNow,
            DateTime.UtcNow));
    }
}
