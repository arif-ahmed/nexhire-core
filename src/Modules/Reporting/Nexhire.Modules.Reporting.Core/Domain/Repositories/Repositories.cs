using Nexhire.Modules.Reporting.Core.Domain.Aggregates;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.ReadModels;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;

namespace Nexhire.Modules.Reporting.Core.Domain.Repositories;

public interface IReportDefinitionRepository
{
    Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ReportDefinition?> GetByIdWithVersionsAsync(Guid id, CancellationToken ct = default);
    Task<List<ReportDefinition>> ListActiveByCategoryAsync(ReportCategory? category, string? roleFilter, CancellationToken ct = default);
    Task AddAsync(ReportDefinition definition, CancellationToken ct = default);
    void Update(ReportDefinition definition);
}

public interface IReportRunRepository
{
    Task<ReportRun?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ReportRun>> GetQueuedAsync(CancellationToken ct = default);
    Task AddAsync(ReportRun run, CancellationToken ct = default);
    void Update(ReportRun run);
}

public interface IReportScheduleRepository
{
    Task<ReportSchedule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ReportSchedule>> GetDueForRunAsync(DateTime nowUtc, CancellationToken ct = default);
    Task<List<ReportSchedule>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(ReportSchedule schedule, CancellationToken ct = default);
    void Update(ReportSchedule schedule);
    void Remove(ReportSchedule schedule);
}

public interface IRetentionPolicyRepository
{
    Task<RetentionPolicy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<RetentionPolicy>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(RetentionPolicy policy, CancellationToken ct = default);
    void Update(RetentionPolicy policy);
}

public interface IAlertRuleRepository
{
    Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<AlertRule>> GetEnabledByMetricKeyAsync(string metricKey, CancellationToken ct = default);
    Task<List<AlertRule>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(AlertRule rule, CancellationToken ct = default);
    void Update(AlertRule rule);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IActivityReadStore
{
    Task UpsertSessionSnapshotAsync(SessionSnapshot snapshot, CancellationToken ct = default);
    Task InsertActivityRecordAsync(ActivityRecord record, CancellationToken ct = default);
    Task<bool> ActivityRecordExistsAsync(Guid sourceEventId, ActivityType activityType, CancellationToken ct = default);
    Task<List<ActivityRecord>> GetActivityRecordsAsync(Guid? userId, ActorRole? role, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);
    Task<List<SessionSnapshot>> GetActiveSessionsAsync(CancellationToken ct = default);
    Task<List<ActivityRecord>> GetOlderThanAsync(DateTime cutoff, RetentionScope scope, CancellationToken ct = default);
    Task DeleteAsync(List<Guid> ids, CancellationToken ct = default);
}

public interface IAnalyticsReadStore
{
    Task UpsertAnalyticsRollupAsync(string metric, RollupGrain grain, DateTime bucketStart,
        string? industry, string? occupationCode, string? skillCode, string? region, Guid? employerId,
        decimal delta, CancellationToken ct = default);
    Task UpsertSkillDemandAsync(string skillCode, string? industry, string? region, RollupGrain grain, DateTime bucketStart, int postingDelta, int supplyDelta, CancellationToken ct = default);
    Task UpsertOutcomeCohortAsync(RollupGrain grain, DateTime bucketStart, string? industry, string? region, string? skillCode, int appDelta, int offerDelta, int acceptDelta, CancellationToken ct = default);
    Task UpsertMatchingMetricAsync(DateTime bucketStart, RollupGrain grain, int recommendationDelta, CancellationToken ct = default);
}

public interface IPerformanceReadStore
{
    Task UpsertSystemMetricBucketAsync(string metricKey, DateTime bucketStart, int bucketSeconds, decimal value, CancellationToken ct = default);
    Task<List<SystemMetricBucket>> GetMetricHistoryAsync(string metricKey, DateTime from, DateTime to, CancellationToken ct = default);
}

public interface IReportAccessLogStore
{
    Task InsertAsync(ReportAccessLog log, CancellationToken ct = default);
    Task<List<ReportAccessLog>> GetAuditAsync(Guid? userId, Guid? reportDefinitionId, int page, int pageSize, CancellationToken ct = default);
}

public interface IInboxStore
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
