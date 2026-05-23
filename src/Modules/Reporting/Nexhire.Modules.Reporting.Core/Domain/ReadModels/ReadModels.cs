using Nexhire.Modules.Reporting.Core.Domain.Enums;

namespace Nexhire.Modules.Reporting.Core.Domain.ReadModels;

public class ActivityRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ActorRole ActorRole { get; set; }
    public ActivityType ActivityType { get; set; }
    public DateTime OccurredOnUtc { get; set; }
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? Metadata { get; set; }
    public Guid SourceEventId { get; set; }
    public DateTime ProjectedOnUtc { get; set; }
}

public class SessionSnapshot
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public ActorRole ActorRole { get; set; }
    public DateTime? LastLoginOnUtc { get; set; }
    public int? LastSessionDurationSeconds { get; set; }
    public int ConcurrentSessionCount { get; set; }
    public bool IsCurrentlyActive { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
}

public class AnalyticsRollup
{
    public Guid Id { get; set; }
    public string Metric { get; set; } = string.Empty;
    public RollupGrain Grain { get; set; }
    public DateTime BucketStartUtc { get; set; }
    public string? Industry { get; set; }
    public string? OccupationCode { get; set; }
    public string? SkillCode { get; set; }
    public string? Region { get; set; }
    public Guid? EmployerId { get; set; }
    public decimal Value { get; set; }
    public int SampleCount { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
}

public class SalaryStatRollup
{
    public Guid Id { get; set; }
    public string OccupationCode { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Region { get; set; }
    public RollupGrain Grain { get; set; }
    public DateTime BucketStartUtc { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public decimal? Median { get; set; }
    public decimal? P25 { get; set; }
    public decimal? P75 { get; set; }
    public string Currency { get; set; } = "USD";
    public int SampleCount { get; set; }
}

public class SkillDemandRollup
{
    public Guid Id { get; set; }
    public string SkillCode { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Region { get; set; }
    public RollupGrain Grain { get; set; }
    public DateTime BucketStartUtc { get; set; }
    public int PostingCount { get; set; }
    public int CandidateSupplyCount { get; set; }
    public decimal? GrowthRatePct { get; set; }
    public bool IsEmerging { get; set; }
}

public class SystemMetricBucket
{
    public Guid Id { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public DateTime BucketStartUtc { get; set; }
    public int BucketSeconds { get; set; }
    public decimal? P50 { get; set; }
    public decimal? P95 { get; set; }
    public decimal? P99 { get; set; }
    public decimal? Avg { get; set; }
    public long Count { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
}

public class MatchingMetricRollup
{
    public Guid Id { get; set; }
    public DateTime BucketStartUtc { get; set; }
    public RollupGrain Grain { get; set; }
    public string? JobCategory { get; set; }
    public string? Industry { get; set; }
    public string? Region { get; set; }
    public string? SkillCode { get; set; }
    public int RecommendationCount { get; set; }
    public int SelectedCount { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? PrecisionScore { get; set; }
    public decimal? Recall { get; set; }
    public decimal? AvgSatisfaction { get; set; }
    public decimal? ApplicationRate { get; set; }
    public decimal? OfferRate { get; set; }
    public string? AbTestVariant { get; set; }
}

public class OutcomeCohortRollup
{
    public Guid Id { get; set; }
    public RollupGrain Grain { get; set; }
    public DateTime BucketStartUtc { get; set; }
    public string? Industry { get; set; }
    public string? Region { get; set; }
    public string? SkillCode { get; set; }
    public int ApplicationCount { get; set; }
    public int OfferCount { get; set; }
    public int AcceptanceCount { get; set; }
    public decimal? PlacementRatePct { get; set; }
    public int CareerProgressionCount { get; set; }
}

public class ReportAccessLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public Guid? ReportDefinitionId { get; set; }
    public Guid? ReportRunId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; }
}
