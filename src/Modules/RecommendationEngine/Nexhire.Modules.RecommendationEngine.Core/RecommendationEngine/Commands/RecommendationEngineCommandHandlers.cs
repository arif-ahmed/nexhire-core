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
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.RecommendationEngine.Core.RecommendationEngine.Commands;

public sealed class UpdateMatchThresholdCommandHandler : ICommandHandler<UpdateMatchThresholdCommand>
{
    private readonly IMatchThresholdConfigurationRepository _configRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public UpdateMatchThresholdCommandHandler(
        IMatchThresholdConfigurationRepository configRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateMatchThresholdCommand request, CancellationToken cancellationToken)
    {
        var config = await _configRepository.GetDefaultAsync(cancellationToken);
        if (config == null)
        {
            config = MatchThresholdConfiguration.CreateDefault();
            await _configRepository.AddAsync(config, cancellationToken);
        }

        var result = config.UpdateGlobalThreshold(request.NewThreshold, request.AdminId);
        if (result.IsFailure)
        {
            return result;
        }

        await _configRepository.UpdateAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class UpdateMatchingWeightsCommandHandler : ICommandHandler<UpdateMatchingWeightsCommand>
{
    private readonly IMatchingWeightProfileRepository _weightRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public UpdateMatchingWeightsCommandHandler(
        IMatchingWeightProfileRepository weightRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _weightRepository = weightRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateMatchingWeightsCommand request, CancellationToken cancellationToken)
    {
        var weightsResult = FactorWeights.Create(
            request.Skill, request.Education, request.Training,
            request.Location, request.Experience, request.Salary);

        if (weightsResult.IsFailure)
        {
            return Result.Failure(weightsResult.Error);
        }

        var activeProfiles = await _weightRepository.GetActiveProfilesAsync(cancellationToken);
        var controlProfile = activeProfiles.FirstOrDefault(p => p.VariantId == "control");

        string newVersion = "1.0.0";
        if (controlProfile != null)
        {
            controlProfile.SupersedeBy($"1.{int.Parse(controlProfile.Version.Split('.')[1]) + 1}.0");
            newVersion = controlProfile.SupededVersionString(); // Helper or direct version logic
            newVersion = $"1.{int.Parse(controlProfile.Version.Split('.')[1]) + 1}.0";
            await _weightRepository.UpdateAsync(controlProfile, cancellationToken);
        }

        var newProfile = MatchingWeightProfile.Create(newVersion, weightsResult.Value, "control", 100, request.AdminId);
        newProfile.Activate();

        await _weightRepository.AddAsync(newProfile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public static class MatchingWeightProfileExtensions
{
    public static string SupededVersionString(this MatchingWeightProfile profile)
    {
        return profile.Version;
    }
}

public sealed class CreateTalentPoolCommandHandler : ICommandHandler<CreateTalentPoolCommand, Guid>
{
    private readonly ITalentPoolRepository _poolRepository;
    private readonly IEmployerAccessApi _employerAccessApi;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public CreateTalentPoolCommandHandler(
        ITalentPoolRepository poolRepository,
        IEmployerAccessApi employerAccessApi,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _poolRepository = poolRepository;
        _employerAccessApi = employerAccessApi;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateTalentPoolCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await _employerAccessApi.HasAccessAsync(request.EmployerId, request.RecruiterId, cancellationToken);
        if (!hasAccess)
        {
            return Result.Failure<Guid>(new Error("E-ACCESS-DENIED", "Recruiter does not have permission for this employer."));
        }

        int activeCount = await _poolRepository.GetActivePoolCountForEmployerAsync(request.EmployerId, cancellationToken);
        if (activeCount >= 20)
        {
            return Result.Failure<Guid>(new Error("E-POOL-LIMIT-EXCEEDED", "Employer has reached the limit of 20 active talent pools."));
        }

        var pool = TalentPool.Create(request.EmployerId, request.RecruiterId, request.Name, request.Description, request.Tags, request.IsShared);
        await _poolRepository.AddAsync(pool, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(pool.Id.Value);
    }
}

public sealed class AddCandidateToTalentPoolCommandHandler : ICommandHandler<AddCandidateToTalentPoolCommand>
{
    private readonly ITalentPoolRepository _poolRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public AddCandidateToTalentPoolCommandHandler(
        ITalentPoolRepository poolRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _poolRepository = poolRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddCandidateToTalentPoolCommand request, CancellationToken cancellationToken)
    {
        var pool = await _poolRepository.GetByIdAsync(new TalentPoolId(request.TalentPoolId), cancellationToken);
        if (pool == null)
        {
            return Result.Failure(new Error("E-POOL-NOT-FOUND", "Talent pool not found."));
        }

        var result = pool.AddCandidate(request.JobSeekerId, request.RecruiterId, request.Note);
        if (result.IsFailure)
        {
            return result;
        }

        await _poolRepository.UpdateAsync(pool, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class RemoveCandidateFromTalentPoolCommandHandler : ICommandHandler<RemoveCandidateFromTalentPoolCommand>
{
    private readonly ITalentPoolRepository _poolRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public RemoveCandidateFromTalentPoolCommandHandler(
        ITalentPoolRepository poolRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _poolRepository = poolRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveCandidateFromTalentPoolCommand request, CancellationToken cancellationToken)
    {
        var pool = await _poolRepository.GetByIdAsync(new TalentPoolId(request.TalentPoolId), cancellationToken);
        if (pool == null)
        {
            return Result.Failure(new Error("E-POOL-NOT-FOUND", "Talent pool not found."));
        }

        var result = pool.RemoveCandidate(request.JobSeekerId);
        if (result.IsFailure)
        {
            return result;
        }

        await _poolRepository.UpdateAsync(pool, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class SetQualificationThresholdCommandHandler : ICommandHandler<SetQualificationThresholdCommand>
{
    private readonly IPostingMatchProfileRepository _postingRepository;
    private readonly ICandidateShortlistRepository _shortlistRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public SetQualificationThresholdCommandHandler(
        IPostingMatchProfileRepository postingRepository,
        ICandidateShortlistRepository shortlistRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _postingRepository = postingRepository;
        _shortlistRepository = shortlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetQualificationThresholdCommand request, CancellationToken cancellationToken)
    {
        var posting = await _postingRepository.GetByPostingIdAsync(request.JobPostingId, cancellationToken);
        if (posting == null)
        {
            return Result.Failure(new Error("E-POSTING-PROFILE-NOT-FOUND", "Posting match profile not found."));
        }

        var thresholdResult = QualificationThreshold.Create(
            request.MinOverallMatch,
            request.MinSkillMatch,
            request.MinEducationLevel,
            request.MinExperienceYears,
            request.RequiredCertifications);

        if (thresholdResult.IsFailure)
        {
            return Result.Failure(thresholdResult.Error);
        }

        posting.SetPerPostingThresholdOverride(request.MinOverallMatch);
        await _postingRepository.AddAsync(posting, cancellationToken);

        var shortlist = await _shortlistRepository.GetByPostingIdAsync(request.JobPostingId, cancellationToken);
        if (shortlist == null)
        {
            shortlist = CandidateShortlist.Create(request.JobPostingId);
            await _shortlistRepository.AddAsync(shortlist, cancellationToken);
        }

        shortlist.MarkStale();
        await _shortlistRepository.UpdateAsync(shortlist, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class RecordRecommendationFeedbackCommandHandler : ICommandHandler<RecordRecommendationFeedbackCommand>
{
    private readonly IRecommendationFeedbackRepository _feedbackRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public RecordRecommendationFeedbackCommandHandler(
        IRecommendationFeedbackRepository feedbackRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _feedbackRepository = feedbackRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RecordRecommendationFeedbackCommand request, CancellationToken cancellationToken)
    {
        var feedback = RecommendationFeedback.Record(request.JobSeekerId, request.JobPostingId, request.Signal);
        await _feedbackRepository.AddAsync(feedback, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class RefreshCandidateShortlistCommandHandler : ICommandHandler<RefreshCandidateShortlistCommand>
{
    private readonly IPostingMatchProfileRepository _postingRepository;
    private readonly ISeekerMatchProfileRepository _seekerRepository;
    private readonly IMatchScoreRepository _matchScoreRepository;
    private readonly IMatchingWeightProfileRepository _weightRepository;
    private readonly ICandidateShortlistRepository _shortlistRepository;
    private readonly MatchScoringService _scoringService;
    private readonly CandidateRankingService _rankingService;
    private readonly CandidatePrivacyFilter _privacyFilter;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public RefreshCandidateShortlistCommandHandler(
        IPostingMatchProfileRepository postingRepository,
        ISeekerMatchProfileRepository seekerRepository,
        IMatchScoreRepository matchScoreRepository,
        IMatchingWeightProfileRepository weightRepository,
        ICandidateShortlistRepository shortlistRepository,
        MatchScoringService scoringService,
        CandidateRankingService rankingService,
        CandidatePrivacyFilter privacyFilter,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _postingRepository = postingRepository;
        _seekerRepository = seekerRepository;
        _matchScoreRepository = matchScoreRepository;
        _weightRepository = weightRepository;
        _shortlistRepository = shortlistRepository;
        _scoringService = scoringService;
        _rankingService = rankingService;
        _privacyFilter = privacyFilter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RefreshCandidateShortlistCommand request, CancellationToken cancellationToken)
    {
        var posting = await _postingRepository.GetByPostingIdAsync(request.JobPostingId, cancellationToken);
        if (posting == null)
        {
            return Result.Failure(new Error("E-POSTING-PROFILE-NOT-FOUND", "Posting match profile not found."));
        }

        var shortlist = await _shortlistRepository.GetByPostingIdAsync(request.JobPostingId, cancellationToken);
        if (shortlist == null)
        {
            shortlist = CandidateShortlist.Create(request.JobPostingId);
            await _shortlistRepository.AddAsync(shortlist, cancellationToken);
        }

        shortlist.BeginRefresh();
        await _shortlistRepository.UpdateAsync(shortlist, cancellationToken);

        var activeWeightProfiles = await _weightRepository.GetActiveProfilesAsync(cancellationToken);
        var activeWeights = activeProfilesWeights(activeWeightProfiles);

        var seekers = await _seekerRepository.GetActiveProfilesAsync(cancellationToken);
        var matchScores = new Dictionary<Guid, MatchScore>();
        var visibleSeekers = new List<SeekerMatchProfile>();
        var updates = new Dictionary<Guid, DateTime>();

        foreach (var seeker in seekers)
        {
            bool hasApplied = false; // Resolved in actual implementation using job applications repository or direct check

            // Privacy check
            if (!_privacyFilter.IsVisible(seeker, posting, hasApplied, "Shortlist", out _))
            {
                continue;
            }

            visibleSeekers.Add(seeker);
            updates[seeker.JobSeekerId] = DateTime.UtcNow; // Mock update time for sorting tie breaker

            var matchScore = await _matchScoreRepository.GetByKeysAsync(seeker.JobSeekerId, posting.JobPostingId, cancellationToken);
            if (matchScore == null)
            {
                var breakdownResult = await _scoringService.ComputeBreakdownAsync(seeker, posting, cancellationToken);
                if (breakdownResult.IsSuccess)
                {
                    matchScore = MatchScore.Compute(seeker.JobSeekerId, posting.JobPostingId, activeWeights, breakdownResult.Value);
                    await _matchScoreRepository.AddAsync(matchScore, cancellationToken);
                }
            }
            else if (matchScore.IsStale)
            {
                var breakdownResult = await _scoringService.ComputeBreakdownAsync(seeker, posting, cancellationToken);
                if (breakdownResult.IsSuccess)
                {
                    matchScore.Recompute(activeWeights, breakdownResult.Value);
                    await _matchScoreRepository.UpdateAsync(matchScore, cancellationToken);
                }
            }

            if (matchScore != null)
            {
                matchScores[seeker.JobSeekerId] = matchScore;
            }
        }

        var threshold = QualificationThreshold.Create(
            posting.PerPostingThresholdOverride ?? 60,
            50,
            EducationLevel.None,
            0,
            new List<string>()).Value;

        var rankedCandidates = _rankingService.RankAndFilterCandidates(
            posting,
            visibleSeekers,
            matchScores,
            threshold,
            new HashSet<Guid>(),
            updates);

        shortlist.CompleteRefresh(rankedCandidates);
        await _shortlistRepository.UpdateAsync(shortlist, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private MatchingWeightProfile activeProfilesWeights(List<MatchingWeightProfile> profiles)
    {
        return profiles.FirstOrDefault(p => p.VariantId == "control") ?? MatchingWeightProfile.CreateInitial();
    }
}

public sealed class SetPerPostingThresholdCommandHandler : ICommandHandler<SetPerPostingThresholdCommand>
{
    private readonly IPostingMatchProfileRepository _postingRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public SetPerPostingThresholdCommandHandler(
        IPostingMatchProfileRepository postingRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _postingRepository = postingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetPerPostingThresholdCommand request, CancellationToken cancellationToken)
    {
        var posting = await _postingRepository.GetByPostingIdAsync(request.PostingId, cancellationToken);
        if (posting == null)
        {
            return Result.Failure(new Error("E-POSTING-PROFILE-NOT-FOUND", "Posting match profile not found."));
        }

        posting.SetPerPostingThresholdOverride(request.Percent);
        await _postingRepository.AddAsync(posting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class CreateWeightVariantCommandHandler : ICommandHandler<CreateWeightVariantCommand>
{
    private readonly IMatchingWeightProfileRepository _weightRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public CreateWeightVariantCommandHandler(
        IMatchingWeightProfileRepository weightRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _weightRepository = weightRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CreateWeightVariantCommand request, CancellationToken cancellationToken)
    {
        var weightsResult = FactorWeights.Create(
            request.Skill, request.Education, request.Training,
            request.Location, request.Experience, request.Salary);

        if (weightsResult.IsFailure)
        {
            return Result.Failure(weightsResult.Error);
        }

        var profile = MatchingWeightProfile.Create(
            request.Version, weightsResult.Value, request.VariantId,
            request.AllocationPercent, request.CreatedBy);

        await _weightRepository.AddAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class ActivateWeightProfileCommandHandler : ICommandHandler<ActivateWeightProfileCommand>
{
    private readonly IMatchingWeightProfileRepository _weightRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public ActivateWeightProfileCommandHandler(
        IMatchingWeightProfileRepository weightRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _weightRepository = weightRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ActivateWeightProfileCommand request, CancellationToken cancellationToken)
    {
        var target = await _weightRepository.GetByVersionAsync(request.Version, cancellationToken);
        if (target == null)
        {
            return Result.Failure(new Error("E-WEIGHTS-NOT-FOUND", "Weight profile version not found."));
        }

        var activeProfiles = await _weightRepository.GetActiveProfilesAsync(cancellationToken);
        var currentActive = activeProfiles.FirstOrDefault(p => p.VariantId == target.VariantId);
        if (currentActive != null && currentActive.Id != target.Id)
        {
            currentActive.SupersedeBy(target.Version);
            await _weightRepository.UpdateAsync(currentActive, cancellationToken);
        }

        target.Activate();
        await _weightRepository.UpdateAsync(target, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class RollbackWeightProfileCommandHandler : ICommandHandler<RollbackWeightProfileCommand>
{
    private readonly IMatchingWeightProfileRepository _weightRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public RollbackWeightProfileCommandHandler(
        IMatchingWeightProfileRepository weightRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _weightRepository = weightRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RollbackWeightProfileCommand request, CancellationToken cancellationToken)
    {
        var target = await _weightRepository.GetByVersionAsync(request.TargetVersion, cancellationToken);
        if (target == null)
        {
            return Result.Failure(new Error("E-WEIGHTS-ROLLBACK-UNKNOWN", "Target version not found."));
        }

        var activeProfiles = await _weightRepository.GetActiveProfilesAsync(cancellationToken);
        var currentActive = activeProfiles.FirstOrDefault(p => p.VariantId == target.VariantId);
        if (currentActive != null && currentActive.Id != target.Id)
        {
            currentActive.SupersedeBy(target.Version);
            await _weightRepository.UpdateAsync(currentActive, cancellationToken);
        }

        target.Activate();
        await _weightRepository.UpdateAsync(target, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class SetShortlistSizeCommandHandler : ICommandHandler<SetShortlistSizeCommand>
{
    private readonly ICandidateShortlistRepository _shortlistRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public SetShortlistSizeCommandHandler(
        ICandidateShortlistRepository shortlistRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _shortlistRepository = shortlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetShortlistSizeCommand request, CancellationToken cancellationToken)
    {
        if (request.Size <= 0)
        {
            return Result.Failure(new Error("E-SHORTLIST-INVALID-SIZE", "Shortlist size must be greater than zero."));
        }

        var shortlist = await _shortlistRepository.GetByPostingIdAsync(request.PostingId, cancellationToken);
        if (shortlist == null)
        {
            return Result.Failure(new Error("E-SHORTLIST-NOT-FOUND", "Candidate shortlist not found for this posting."));
        }

        shortlist.SetConfiguredSize(request.Size);
        await _shortlistRepository.UpdateAsync(shortlist, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class UpdateTalentPoolCommandHandler : ICommandHandler<UpdateTalentPoolCommand>
{
    private readonly ITalentPoolRepository _poolRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public UpdateTalentPoolCommandHandler(
        ITalentPoolRepository poolRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _poolRepository = poolRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateTalentPoolCommand request, CancellationToken cancellationToken)
    {
        var pool = await _poolRepository.GetByIdAsync(new TalentPoolId(request.PoolId), cancellationToken);
        if (pool == null)
        {
            return Result.Failure(new Error("E-POOL-NOT-FOUND", "Talent pool not found."));
        }

        if (request.Name != null) pool.Rename(request.Name);
        if (request.Description != null) pool.UpdateDescription(request.Description);
        if (request.AssociatedSkills != null) pool.SetAssociatedSkills(request.AssociatedSkills);
        if (request.IsShared.HasValue) pool.SetShared(request.IsShared.Value);

        await _poolRepository.UpdateAsync(pool, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class UpdateTalentPoolCandidateNoteCommandHandler : ICommandHandler<UpdateTalentPoolCandidateNoteCommand>
{
    private readonly ITalentPoolRepository _poolRepository;
    private readonly IRecommendationEngineUnitOfWork _unitOfWork;

    public UpdateTalentPoolCandidateNoteCommandHandler(
        ITalentPoolRepository poolRepository,
        IRecommendationEngineUnitOfWork unitOfWork)
    {
        _poolRepository = poolRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateTalentPoolCandidateNoteCommand request, CancellationToken cancellationToken)
    {
        var pool = await _poolRepository.GetByIdAsync(new TalentPoolId(request.PoolId), cancellationToken);
        if (pool == null)
        {
            return Result.Failure(new Error("E-POOL-NOT-FOUND", "Talent pool not found."));
        }

        pool.UpdateCandidateNote(request.JobSeekerId, request.Note);
        await _poolRepository.UpdateAsync(pool, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
