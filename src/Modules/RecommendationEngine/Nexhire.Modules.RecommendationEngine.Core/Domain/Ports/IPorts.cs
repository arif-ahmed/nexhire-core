using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Ports;

public interface IEmbeddingModelPort
{
    Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

public interface IVectorIndexPort
{
    Task<decimal> GetSimilarityAsync(EmbeddingVector v1, EmbeddingVector v2, CancellationToken cancellationToken = default);
    Task<decimal> GetSkillSimilarityAsync(string taxonomyCode1, string taxonomyCode2, CancellationToken cancellationToken = default);
}

public interface INlpExtractionPort
{
    Task<List<SkillRequirement>> ExtractSkillsAsync(string text, CancellationToken cancellationToken = default);
}

public interface IEmployerAccessApi
{
    Task<bool> HasAccessAsync(Guid employerId, Guid recruiterId, CancellationToken cancellationToken = default);
}

public interface ICollaborativeFilteringPort
{
    Task<List<Guid>> GetCollaborativeRecommendationsAsync(Guid jobSeekerId, int limit = 20, CancellationToken cancellationToken = default);
}
