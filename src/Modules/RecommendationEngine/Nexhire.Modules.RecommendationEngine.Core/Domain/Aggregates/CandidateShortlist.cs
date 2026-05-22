using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

public sealed class CandidateShortlist : AggregateRoot<CandidateShortlistId>
{
    private readonly List<ShortlistCandidate> _candidates = new();

    public Guid JobPostingId { get; private set; }
    public IReadOnlyCollection<ShortlistCandidate> Candidates => _candidates.AsReadOnly();
    public ShortlistRefreshState RefreshState { get; private set; }
    public DateTime LastRefreshedUtc { get; private set; }

    private CandidateShortlist() : base(CandidateShortlistId.New()) { }

    private CandidateShortlist(
        CandidateShortlistId id,
        Guid jobPostingId,
        List<ShortlistCandidate> candidates,
        ShortlistRefreshState refreshState,
        DateTime lastRefreshedUtc) : base(id)
    {
        JobPostingId = jobPostingId;
        _candidates = candidates ?? new List<ShortlistCandidate>();
        RefreshState = refreshState;
        LastRefreshedUtc = lastRefreshedUtc;
    }

    public static CandidateShortlist Create(Guid jobPostingId)
    {
        return new CandidateShortlist(
            CandidateShortlistId.New(),
            jobPostingId,
            new List<ShortlistCandidate>(),
            ShortlistRefreshState.Fresh,
            DateTime.UtcNow);
    }

    public void BeginRefresh()
    {
        RefreshState = ShortlistRefreshState.Refreshing;
    }

    public void CompleteRefresh(List<ShortlistCandidate> candidates)
    {
        _candidates.Clear();
        if (candidates != null)
        {
            _candidates.AddRange(candidates);
        }
        RefreshState = ShortlistRefreshState.Fresh;
        LastRefreshedUtc = DateTime.UtcNow;
    }

    public void MarkStale()
    {
        RefreshState = ShortlistRefreshState.Stale;
    }

    public int ConfiguredSize { get; private set; } = 100;

    public void SetConfiguredSize(int size)
    {
        ConfiguredSize = size;
    }
}

public sealed class ShortlistCandidate : Entity<Guid>
{
    public Guid JobSeekerId { get; private set; }
    public int OverallMatchScore { get; private set; }
    public ShortlistInclusionReason InclusionReason { get; private set; }
    public FitAnalysis FitAnalysis { get; private set; } = null!;
    public DateTime? AppliedAtUtc { get; private set; }

    private ShortlistCandidate() : base(Guid.NewGuid()) { }

    public ShortlistCandidate(
        Guid jobSeekerId,
        int overallMatchScore,
        ShortlistInclusionReason inclusionReason,
        FitAnalysis fitAnalysis,
        DateTime? appliedAtUtc) : base(Guid.NewGuid())
    {
        JobSeekerId = jobSeekerId;
        OverallMatchScore = overallMatchScore;
        InclusionReason = inclusionReason;
        FitAnalysis = fitAnalysis;
        AppliedAtUtc = appliedAtUtc;
    }
}
