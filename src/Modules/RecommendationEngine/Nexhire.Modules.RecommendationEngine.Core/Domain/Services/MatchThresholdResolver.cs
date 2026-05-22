using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Services;

public sealed class MatchThresholdResolver
{
    public int ResolveThreshold(PostingMatchProfile posting, MatchThresholdConfiguration globalConfig)
    {
        if (posting.PerPostingThresholdOverride.HasValue)
        {
            return posting.PerPostingThresholdOverride.Value;
        }

        return globalConfig.GlobalThresholdPercent;
    }
}
