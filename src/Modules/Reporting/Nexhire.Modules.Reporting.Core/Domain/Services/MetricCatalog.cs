namespace Nexhire.Modules.Reporting.Core.Domain.Services;

public record MetricDescriptor(string Key, string Unit, string Description, string ReadModel);

public static class MetricCatalog
{
    private static readonly Dictionary<string, MetricDescriptor> _metrics = new()
    {
        ["posting.volume"] = new("posting.volume", "count", "Job posting volume", "analytics_rollups"),
        ["application.count"] = new("application.count", "count", "Application count", "analytics_rollups"),
        ["hire.count"] = new("hire.count", "count", "Hire count", "analytics_rollups"),
        ["time_to_fill.avg"] = new("time_to_fill.avg", "days", "Average time to fill", "analytics_rollups"),
        ["registration.count"] = new("registration.count", "count", "Registration count", "analytics_rollups"),
        ["employer.verified.count"] = new("employer.verified.count", "count", "Verified employers", "analytics_rollups"),
        ["profile.l2_completed.count"] = new("profile.l2_completed.count", "count", "L2 completed profiles", "analytics_rollups"),
        ["search.count"] = new("search.count", "count", "Search count", "analytics_rollups"),
        ["notification.dispatched.count"] = new("notification.dispatched.count", "count", "Notifications dispatched", "analytics_rollups"),
        ["notification.delivered.count"] = new("notification.delivered.count", "count", "Notifications delivered", "analytics_rollups"),
        ["notification.failed.count"] = new("notification.failed.count", "count", "Notifications failed", "analytics_rollups"),
        ["notification.digest.count"] = new("notification.digest.count", "count", "Digests sent", "analytics_rollups"),
        ["help.feedback.count"] = new("help.feedback.count", "count", "Help feedback count", "analytics_rollups"),
        ["activity.count"] = new("activity.count", "count", "Activity count", "activity_records"),
        ["salary.median"] = new("salary.median", "currency", "Median salary", "salary_stat_rollups"),
        ["match.accuracy"] = new("match.accuracy", "ratio", "Match accuracy", "matching_metric_rollups"),
        ["match.precision"] = new("match.precision", "ratio", "Match precision", "matching_metric_rollups"),
        ["match.recall"] = new("match.recall", "ratio", "Match recall", "matching_metric_rollups"),
        ["api.latency.p95"] = new("api.latency.p95", "ms", "API latency p95", "system_metric_buckets"),
        ["system.error_rate"] = new("system.error_rate", "ratio", "System error rate", "system_metric_buckets"),
        ["system.cpu"] = new("system.cpu", "percent", "CPU utilization", "system_metric_buckets"),
        ["system.memory"] = new("system.memory", "percent", "Memory utilization", "system_metric_buckets"),
        ["integration.error_rate"] = new("integration.error_rate", "ratio", "Integration error rate", "system_metric_buckets"),
    };

    private static readonly HashSet<string> _dimensions = new()
    {
        "industry", "occupation_code", "region", "employer_id", "skill_code",
        "job_category", "grain", "actor_role", "activity_type", "ab_test_variant"
    };

    public static bool IsKnownMetric(string key) => _metrics.ContainsKey(key);
    public static bool IsKnownDimension(string key) => _dimensions.Contains(key);
    public static MetricDescriptor? Describe(string key) => _metrics.GetValueOrDefault(key);
    public static IEnumerable<MetricDescriptor> All() => _metrics.Values;
}
