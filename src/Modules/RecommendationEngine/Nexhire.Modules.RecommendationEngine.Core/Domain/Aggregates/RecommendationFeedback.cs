using System;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

public sealed class RecommendationFeedback : AggregateRoot<RecommendationFeedbackId>
{
    public Guid JobSeekerId { get; private set; }
    public Guid JobPostingId { get; private set; }
    public FeedbackSignal Signal { get; private set; }
    public DateTime? SuppressUntilUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }

    private RecommendationFeedback() : base(RecommendationFeedbackId.New()) { }

    private RecommendationFeedback(
        RecommendationFeedbackId id,
        Guid jobSeekerId,
        Guid jobPostingId,
        FeedbackSignal signal,
        DateTime? suppressUntilUtc,
        DateTime recordedAtUtc) : base(id)
    {
        JobSeekerId = jobSeekerId;
        JobPostingId = jobPostingId;
        Signal = signal;
        SuppressUntilUtc = suppressUntilUtc;
        RecordedAtUtc = recordedAtUtc;
    }

    public static RecommendationFeedback Record(Guid jobSeekerId, Guid jobPostingId, FeedbackSignal signal)
    {
        DateTime? suppressUntilUtc = null;
        if (signal == FeedbackSignal.NotInterested)
        {
            suppressUntilUtc = DateTime.UtcNow.AddDays(14);
        }

        return new RecommendationFeedback(
            RecommendationFeedbackId.New(),
            jobSeekerId,
            jobPostingId,
            signal,
            suppressUntilUtc,
            DateTime.UtcNow);
    }
}
