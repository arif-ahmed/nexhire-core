using System;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

public sealed class MatchingWeightProfile : AggregateRoot<MatchingWeightProfileId>
{
    public string Version { get; private set; } = null!;
    public FactorWeights Weights { get; private set; } = null!;
    public string VariantId { get; private set; } = null!;
    public int VariantAllocationPercent { get; private set; }
    public Guid CreatedBy { get; private set; }
    public bool IsActive { get; private set; }
    public string? SupersededByVersion { get; private set; }

    private MatchingWeightProfile() : base(MatchingWeightProfileId.New()) { }

    private MatchingWeightProfile(
        MatchingWeightProfileId id,
        string version,
        FactorWeights weights,
        string variantId,
        int variantAllocationPercent,
        Guid createdBy,
        bool isActive) : base(id)
    {
        Version = version;
        Weights = weights;
        VariantId = variantId;
        VariantAllocationPercent = variantAllocationPercent;
        CreatedBy = createdBy;
        IsActive = isActive;
    }

    public static MatchingWeightProfile CreateInitial()
    {
        var weights = FactorWeights.Create(0.25m, 0.15m, 0.10m, 0.15m, 0.20m, 0.15m).Value;
        return new MatchingWeightProfile(
            MatchingWeightProfileId.New(),
            "1.0.0",
            weights,
            "control",
            100,
            Guid.Empty,
            isActive: true);
    }

    public static MatchingWeightProfile Create(
        string version,
        FactorWeights weights,
        string variantId,
        int variantAllocationPercent,
        Guid createdBy)
    {
        return new MatchingWeightProfile(
            MatchingWeightProfileId.New(),
            version,
            weights,
            variantId,
            variantAllocationPercent,
            createdBy,
            isActive: false);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void SupersedeBy(string newVersion)
    {
        IsActive = false;
        SupersededByVersion = newVersion;
    }
}
