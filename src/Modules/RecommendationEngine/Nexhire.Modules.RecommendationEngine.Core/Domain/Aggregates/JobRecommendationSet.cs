using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

public sealed class JobRecommendationSet : AggregateRoot<JobRecommendationSetId>
{
    private readonly List<RecommendedJob> _recommendations = new();

    public Guid JobSeekerId { get; private set; }
    public IReadOnlyCollection<RecommendedJob> Recommendations => _recommendations.AsReadOnly();
    public DateTime ComputedAtUtc { get; private set; }

    private JobRecommendationSet() : base(JobRecommendationSetId.New()) { }

    private JobRecommendationSet(
        JobRecommendationSetId id,
        Guid jobSeekerId,
        List<RecommendedJob> recommendations,
        DateTime computedAtUtc) : base(id)
    {
        JobSeekerId = jobSeekerId;
        _recommendations = recommendations ?? new List<RecommendedJob>();
        ComputedAtUtc = computedAtUtc;
    }

    public static JobRecommendationSet Create(Guid jobSeekerId, List<RecommendedJob> recommendations)
    {
        return new JobRecommendationSet(
            JobRecommendationSetId.New(),
            jobSeekerId,
            recommendations,
            DateTime.UtcNow);
    }

    public void UpdateRecommendations(List<RecommendedJob> recommendations)
    {
        _recommendations.Clear();
        if (recommendations != null)
        {
            _recommendations.AddRange(recommendations);
        }
        ComputedAtUtc = DateTime.UtcNow;
    }
}

public sealed class RecommendedJob : Entity<Guid>
{
    public Guid JobPostingId { get; private set; }
    public int MatchScore { get; private set; }
    public decimal HybridScore { get; private set; }
    public RecommendationReason Reason { get; private set; } = null!;
    public bool IsSuppressed { get; private set; }

    private RecommendedJob() : base(Guid.NewGuid()) { }

    public RecommendedJob(
        Guid jobPostingId,
        int matchScore,
        decimal hybridScore,
        RecommendationReason reason,
        bool isSuppressed) : base(Guid.NewGuid())
    {
        JobPostingId = jobPostingId;
        MatchScore = matchScore;
        HybridScore = hybridScore;
        Reason = reason;
        IsSuppressed = isSuppressed;
    }
}
