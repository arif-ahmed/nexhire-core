using Nexhire.Modules.RecommendationEngine.Core.Domain.Ports;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Adapters;

public sealed class StubVectorIndexPort : IVectorIndexPort
{
    public Task<decimal> GetSimilarityAsync(EmbeddingVector v1, EmbeddingVector v2, CancellationToken cancellationToken = default)
    {
        var a = v1.Values;
        var b = v2.Values;

        decimal dotProduct = 0m;
        decimal normA = 0m;
        decimal normB = 0m;

        for (int i = 0; i < a.Count; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denominator = (decimal)Math.Sqrt((double)(normA * normB));
        var cosine = denominator == 0m ? 0m : dotProduct / denominator;

        return Task.FromResult(cosine);
    }

    public Task<decimal> GetSkillSimilarityAsync(string taxonomyCode1, string taxonomyCode2, CancellationToken cancellationToken = default)
    {
        if (taxonomyCode1 == taxonomyCode2)
            return Task.FromResult(1.0m);

        return Task.FromResult(0.5m);
    }
}
