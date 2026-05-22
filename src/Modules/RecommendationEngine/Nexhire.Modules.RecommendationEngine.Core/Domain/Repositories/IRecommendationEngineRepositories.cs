using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Repositories;

public interface ISeekerMatchProfileRepository
{
    Task<SeekerMatchProfile?> GetByIdAsync(SeekerMatchProfileId id, CancellationToken cancellationToken);
    Task<SeekerMatchProfile?> GetBySeekerIdAsync(Guid seekerId, CancellationToken cancellationToken);
    Task<List<SeekerMatchProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken);
    Task AddAsync(SeekerMatchProfile profile, CancellationToken cancellationToken);
}

public interface IPostingMatchProfileRepository
{
    Task<PostingMatchProfile?> GetByIdAsync(PostingMatchProfileId id, CancellationToken cancellationToken);
    Task<PostingMatchProfile?> GetByPostingIdAsync(Guid postingId, CancellationToken cancellationToken);
    Task<List<PostingMatchProfile>> GetActivePostingsAsync(CancellationToken cancellationToken);
    Task AddAsync(PostingMatchProfile profile, CancellationToken cancellationToken);
}

public interface IEmbeddingRecordRepository
{
    Task<EmbeddingRecord?> GetByIdAsync(EmbeddingRecordId id, CancellationToken cancellationToken);
    Task<EmbeddingRecord?> GetByOwnerIdAsync(Guid ownerId, EmbeddingOwnerType ownerType, CancellationToken cancellationToken);
    Task<List<EmbeddingRecord>> GetPendingUploadsAsync(CancellationToken cancellationToken);
    Task AddAsync(EmbeddingRecord record, CancellationToken cancellationToken);
    Task UpdateAsync(EmbeddingRecord record, CancellationToken cancellationToken);
}

public interface IMatchScoreRepository
{
    Task<MatchScore?> GetByIdAsync(MatchScoreId id, CancellationToken cancellationToken);
    Task<MatchScore?> GetByKeysAsync(Guid jobSeekerId, Guid jobPostingId, CancellationToken cancellationToken);
    Task<List<MatchScore>> GetByPostingIdAsync(Guid jobPostingId, CancellationToken cancellationToken);
    Task<List<MatchScore>> GetBySeekerIdAsync(Guid jobSeekerId, CancellationToken cancellationToken);
    Task AddAsync(MatchScore score, CancellationToken cancellationToken);
    Task UpdateAsync(MatchScore score, CancellationToken cancellationToken);
}

public interface IJobRecommendationSetRepository
{
    Task<JobRecommendationSet?> GetByIdAsync(JobRecommendationSetId id, CancellationToken cancellationToken);
    Task<JobRecommendationSet?> GetBySeekerIdAsync(Guid jobSeekerId, CancellationToken cancellationToken);
    Task AddAsync(JobRecommendationSet set, CancellationToken cancellationToken);
    Task UpdateAsync(JobRecommendationSet set, CancellationToken cancellationToken);
}

public interface ICandidateShortlistRepository
{
    Task<CandidateShortlist?> GetByIdAsync(CandidateShortlistId id, CancellationToken cancellationToken);
    Task<CandidateShortlist?> GetByPostingIdAsync(Guid jobPostingId, CancellationToken cancellationToken);
    Task AddAsync(CandidateShortlist shortlist, CancellationToken cancellationToken);
    Task UpdateAsync(CandidateShortlist shortlist, CancellationToken cancellationToken);
}

public interface IMatchingWeightProfileRepository
{
    Task<MatchingWeightProfile?> GetByIdAsync(MatchingWeightProfileId id, CancellationToken cancellationToken);
    Task<MatchingWeightProfile?> GetByVersionAsync(string version, CancellationToken cancellationToken);
    Task<List<MatchingWeightProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken);
    Task<List<MatchingWeightProfile>> GetHistoricalVersionsAsync(string variantId, int limit, CancellationToken cancellationToken);
    Task AddAsync(MatchingWeightProfile profile, CancellationToken cancellationToken);
    Task UpdateAsync(MatchingWeightProfile profile, CancellationToken cancellationToken);
}

public interface IMatchThresholdConfigurationRepository
{
    Task<MatchThresholdConfiguration?> GetDefaultAsync(CancellationToken cancellationToken);
    Task AddAsync(MatchThresholdConfiguration config, CancellationToken cancellationToken);
    Task UpdateAsync(MatchThresholdConfiguration config, CancellationToken cancellationToken);
}

public interface ITalentPoolRepository
{
    Task<TalentPool?> GetByIdAsync(TalentPoolId id, CancellationToken cancellationToken);
    Task<List<TalentPool>> GetByEmployerIdAsync(Guid employerId, CancellationToken cancellationToken);
    Task<int> GetActivePoolCountForEmployerAsync(Guid employerId, CancellationToken cancellationToken);
    Task AddAsync(TalentPool pool, CancellationToken cancellationToken);
    Task UpdateAsync(TalentPool pool, CancellationToken cancellationToken);
}

public interface IRecommendationFeedbackRepository
{
    Task<List<RecommendationFeedback>> GetFeedbackForSeekerAsync(Guid jobSeekerId, CancellationToken cancellationToken);
    Task AddAsync(RecommendationFeedback feedback, CancellationToken cancellationToken);
}

public interface IRecommendationEngineUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
