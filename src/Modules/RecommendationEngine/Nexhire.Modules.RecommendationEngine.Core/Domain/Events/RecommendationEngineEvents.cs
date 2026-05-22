using System;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Events;

public abstract record RecommendationEngineEvent(DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

public sealed record SeekerMatchInputChanged(Guid SeekerId, DateTime OccurredOnUtc) : RecommendationEngineEvent(OccurredOnUtc);
public sealed record SeekerPrivacyChanged(Guid SeekerId, PrivacyLevel Privacy, DateTime OccurredOnUtc) : RecommendationEngineEvent(OccurredOnUtc);
public sealed record PostingMatchInputChanged(Guid PostingId, DateTime OccurredOnUtc) : RecommendationEngineEvent(OccurredOnUtc);
public sealed record RecommendationProfileExposed(Guid JobSeekerId, Guid JobPostingId, Guid EmployerId, string ExposureContext, DateTime OccurredOnUtc) : RecommendationEngineEvent(OccurredOnUtc);
