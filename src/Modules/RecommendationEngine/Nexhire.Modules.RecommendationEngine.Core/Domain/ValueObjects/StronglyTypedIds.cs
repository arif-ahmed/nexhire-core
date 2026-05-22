using System;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

public record SeekerMatchProfileId(Guid Value)
{
    public static SeekerMatchProfileId New() => new(Guid.NewGuid());
    public static SeekerMatchProfileId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public record PostingMatchProfileId(Guid Value)
{
    public static PostingMatchProfileId New() => new(Guid.NewGuid());
    public static PostingMatchProfileId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public record EmbeddingRecordId(Guid Value)
{
    public static EmbeddingRecordId New() => new(Guid.NewGuid());
    public static EmbeddingRecordId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public record MatchScoreId(Guid Value)
{
    public static MatchScoreId New() => new(Guid.NewGuid());
    public static MatchScoreId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public record JobRecommendationSetId(Guid Value)
{
    public static JobRecommendationSetId New() => new(Guid.NewGuid());
    public static JobRecommendationSetId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public record CandidateShortlistId(Guid Value)
{
    public static CandidateShortlistId New() => new(Guid.NewGuid());
    public static CandidateShortlistId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public record MatchingWeightProfileId(Guid Value)
{
    public static MatchingWeightProfileId New() => new(Guid.NewGuid());
    public static MatchingWeightProfileId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public record MatchThresholdConfigurationId(Guid Value)
{
    public static MatchThresholdConfigurationId New() => new(Guid.NewGuid());
    public static MatchThresholdConfigurationId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public record TalentPoolId(Guid Value)
{
    public static TalentPoolId New() => new(Guid.NewGuid());
    public static TalentPoolId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public record RecommendationFeedbackId(Guid Value)
{
    public static RecommendationFeedbackId New() => new(Guid.NewGuid());
    public static RecommendationFeedbackId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
