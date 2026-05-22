using Nexhire.Modules.RecommendationEngine.Core.Domain.Ports;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Adapters;

public sealed class StubNlpExtractionPort : INlpExtractionPort
{
    public Task<List<SkillRequirement>> ExtractSkillsAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<SkillRequirement>());
    }
}
