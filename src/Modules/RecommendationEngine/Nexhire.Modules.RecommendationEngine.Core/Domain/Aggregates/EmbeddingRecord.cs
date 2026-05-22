using System;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

public sealed class EmbeddingRecord : AggregateRoot<EmbeddingRecordId>
{
    public Guid OwnerId { get; private set; }
    public EmbeddingOwnerType OwnerType { get; private set; }
    public EmbeddingVector Vector { get; private set; } = null!;
    public string ModelVersion { get; private set; } = null!;
    public DateTime UpdatedAtUtc { get; private set; }

    private EmbeddingRecord() : base(EmbeddingRecordId.New()) { }

    private EmbeddingRecord(
        EmbeddingRecordId id,
        Guid ownerId,
        EmbeddingOwnerType ownerType,
        EmbeddingVector vector,
        string modelVersion,
        DateTime updatedAtUtc) : base(id)
    {
        OwnerId = ownerId;
        OwnerType = ownerType;
        Vector = vector;
        ModelVersion = modelVersion;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static EmbeddingRecord Create(
        Guid ownerId,
        EmbeddingOwnerType ownerType,
        EmbeddingVector vector,
        string modelVersion)
    {
        return new EmbeddingRecord(
            EmbeddingRecordId.New(),
            ownerId,
            ownerType,
            vector,
            modelVersion,
            DateTime.UtcNow);
    }

    public void UpdateEmbedding(EmbeddingVector vector, string modelVersion)
    {
        Vector = vector;
        ModelVersion = modelVersion;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
