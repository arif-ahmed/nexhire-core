using System;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Events;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Services;

public sealed class CandidatePrivacyFilter
{
    public bool IsVisible(
        SeekerMatchProfile seeker,
        PostingMatchProfile posting,
        bool hasApplied,
        string exposureContext,
        out RecommendationProfileExposed? exposureEvent)
    {
        exposureEvent = null;

        if (seeker == null || !seeker.IsActive)
        {
            return false;
        }

        bool isVisible = false;

        switch (seeker.PrivacyLevel)
        {
            case PrivacyLevel.Public:
                isVisible = true;
                break;

            case PrivacyLevel.ApplyOnly:
            case PrivacyLevel.Hidden:
                isVisible = hasApplied;
                break;
        }

        if (isVisible && posting != null)
        {
            exposureEvent = new RecommendationProfileExposed(
                seeker.JobSeekerId,
                posting.JobPostingId,
                posting.EmployerId,
                exposureContext,
                DateTime.UtcNow);
        }

        return isVisible;
    }
}
