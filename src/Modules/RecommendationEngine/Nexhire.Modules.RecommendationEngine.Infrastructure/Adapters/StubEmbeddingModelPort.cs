using Nexhire.Modules.RecommendationEngine.Core.Domain.Ports;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Infrastructure.Adapters;

public sealed class StubEmbeddingModelPort : IEmbeddingModelPort
{
    private const int Dimension = 768;

    public Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var seed = text.GetHashCode(StringComparison.Ordinal);
        var random = new Random(seed);

        var values = Enumerable.Range(0, Dimension)
            .Select(_ => (decimal)random.NextDouble())
            .ToList();

        var result = EmbeddingVector.Create(values, Dimension).Value;
        return Task.FromResult(result);
    }
}
