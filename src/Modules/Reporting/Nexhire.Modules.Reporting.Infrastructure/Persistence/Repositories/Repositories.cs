using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.Reporting.Core.Domain.Aggregates;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.ReadModels;
using Nexhire.Modules.Reporting.Core.Domain.Repositories;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.Reporting.Infrastructure.Persistence.Repositories;

public class ReportDefinitionRepository : IReportDefinitionRepository
{
    private readonly ReportingDbContext _ctx;
    public ReportDefinitionRepository(ReportingDbContext ctx) { _ctx = ctx; }
    public Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct) => _ctx.ReportDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<ReportDefinition?> GetByIdWithVersionsAsync(Guid id, CancellationToken ct) => _ctx.ReportDefinitions.Include(x => x.Versions).FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<List<ReportDefinition>> ListActiveByCategoryAsync(ReportCategory? category, string? roleFilter, CancellationToken ct)
    {
        var q = _ctx.ReportDefinitions.Where(x => x.Status == ReportDefinitionStatus.Active);
        if (category.HasValue) q = q.Where(x => x.Category == category.Value);
        return await q.ToListAsync(ct);
    }
    public async Task AddAsync(ReportDefinition definition, CancellationToken ct) => await _ctx.ReportDefinitions.AddAsync(definition, ct);
    public void Update(ReportDefinition definition) => _ctx.ReportDefinitions.Update(definition);
}

public class ReportRunRepository : IReportRunRepository
{
    private readonly ReportingDbContext _ctx;
    public ReportRunRepository(ReportingDbContext ctx) { _ctx = ctx; }
    public Task<ReportRun?> GetByIdAsync(Guid id, CancellationToken ct) => _ctx.ReportRuns.Include(x => x.Artifacts).FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<List<ReportRun>> GetQueuedAsync(CancellationToken ct) => _ctx.ReportRuns.Where(x => x.Status == ReportRunStatus.Queued).ToListAsync(ct);
    public async Task AddAsync(ReportRun run, CancellationToken ct) => await _ctx.ReportRuns.AddAsync(run, ct);
    public void Update(ReportRun run) => _ctx.ReportRuns.Update(run);
}

public class ReportScheduleRepository : IReportScheduleRepository
{
    private readonly ReportingDbContext _ctx;
    public ReportScheduleRepository(ReportingDbContext ctx) { _ctx = ctx; }
    public Task<ReportSchedule?> GetByIdAsync(Guid id, CancellationToken ct) => _ctx.ReportSchedules.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<List<ReportSchedule>> GetDueForRunAsync(DateTime nowUtc, CancellationToken ct) => _ctx.ReportSchedules.Where(x => x.Status == ScheduleStatus.Active && x.NextRunOnUtc <= nowUtc).ToListAsync(ct);
    public Task<List<ReportSchedule>> ListAllAsync(CancellationToken ct) => _ctx.ReportSchedules.ToListAsync(ct);
    public async Task AddAsync(ReportSchedule schedule, CancellationToken ct) => await _ctx.ReportSchedules.AddAsync(schedule, ct);
    public void Update(ReportSchedule schedule) => _ctx.ReportSchedules.Update(schedule);
    public void Remove(ReportSchedule schedule) => _ctx.ReportSchedules.Remove(schedule);
}

public class RetentionPolicyRepository : IRetentionPolicyRepository
{
    private readonly ReportingDbContext _ctx;
    public RetentionPolicyRepository(ReportingDbContext ctx) { _ctx = ctx; }
    public Task<RetentionPolicy?> GetByIdAsync(Guid id, CancellationToken ct) => _ctx.RetentionPolicies.Include(x => x.Versions).Include(x => x.Runs).FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<List<RetentionPolicy>> GetActiveAsync(CancellationToken ct) => _ctx.RetentionPolicies.Where(x => x.Status == RetentionPolicyStatus.Active).ToListAsync(ct);
    public async Task AddAsync(RetentionPolicy policy, CancellationToken ct) => await _ctx.RetentionPolicies.AddAsync(policy, ct);
    public void Update(RetentionPolicy policy) => _ctx.RetentionPolicies.Update(policy);
}

public class AlertRuleRepository : IAlertRuleRepository
{
    private readonly ReportingDbContext _ctx;
    public AlertRuleRepository(ReportingDbContext ctx) { _ctx = ctx; }
    public Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken ct) => _ctx.AlertRules.Include(x => x.Incidents).FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<List<AlertRule>> GetEnabledByMetricKeyAsync(string metricKey, CancellationToken ct) => _ctx.AlertRules.Include(x => x.Incidents).Where(x => x.MetricKey == metricKey && x.Status == AlertRuleStatus.Enabled).ToListAsync(ct);
    public Task<List<AlertRule>> ListAllAsync(CancellationToken ct) => _ctx.AlertRules.Include(x => x.Incidents).ToListAsync(ct);
    public async Task AddAsync(AlertRule rule, CancellationToken ct) => await _ctx.AlertRules.AddAsync(rule, ct);
    public void Update(AlertRule rule) => _ctx.AlertRules.Update(rule);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ReportingDbContext _ctx;
    public UnitOfWork(ReportingDbContext ctx) { _ctx = ctx; }
    public Task<int> SaveChangesAsync(CancellationToken ct) => _ctx.SaveChangesAsync(ct);
}

public class ActivityReadStore : IActivityReadStore
{
    private readonly ReportingDbContext _ctx;
    public ActivityReadStore(ReportingDbContext ctx) { _ctx = ctx; }

    public async Task UpsertSessionSnapshotAsync(SessionSnapshot snapshot, CancellationToken ct)
    {
        var existing = await _ctx.SessionSnapshots.FindAsync(new object[] { snapshot.UserId }, ct);
        if (existing is null)
        {
            snapshot.ConcurrentSessionCount = 1;
            await _ctx.SessionSnapshots.AddAsync(snapshot, ct);
        }
        else
        {
            existing.DisplayName = snapshot.DisplayName;
            existing.ActorRole = snapshot.ActorRole;
            existing.LastLoginOnUtc = snapshot.LastLoginOnUtc;
            existing.IsCurrentlyActive = snapshot.IsCurrentlyActive;
            existing.ConcurrentSessionCount++;
            existing.UpdatedOnUtc = DateTime.UtcNow;
            _ctx.SessionSnapshots.Update(existing);
        }
    }

    public async Task InsertActivityRecordAsync(ActivityRecord record, CancellationToken ct)
        => await _ctx.ActivityRecords.AddAsync(record, ct);

    public Task<bool> ActivityRecordExistsAsync(Guid sourceEventId, ActivityType activityType, CancellationToken ct)
        => _ctx.ActivityRecords.AnyAsync(r => r.SourceEventId == sourceEventId && r.ActivityType == activityType, ct);

    public async Task<List<ActivityRecord>> GetActivityRecordsAsync(Guid? userId, ActorRole? role, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        var q = _ctx.ActivityRecords.AsQueryable();
        if (userId.HasValue) q = q.Where(r => r.UserId == userId.Value);
        if (role.HasValue) q = q.Where(r => r.ActorRole == role.Value);
        if (from.HasValue) q = q.Where(r => r.OccurredOnUtc >= from.Value);
        if (to.HasValue) q = q.Where(r => r.OccurredOnUtc <= to.Value);
        return await q.OrderByDescending(r => r.OccurredOnUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public Task<List<SessionSnapshot>> GetActiveSessionsAsync(CancellationToken ct)
        => _ctx.SessionSnapshots.Where(s => s.IsCurrentlyActive).ToListAsync(ct);

    public async Task<List<ActivityRecord>> GetOlderThanAsync(DateTime cutoff, Core.Domain.ValueObjects.RetentionScope scope, CancellationToken ct)
    {
        var q = _ctx.ActivityRecords.Where(r => r.OccurredOnUtc < cutoff && r.ActorRole == scope.ActorRole);
        if (scope.ActivityTypes.Any()) q = q.Where(r => scope.ActivityTypes.Contains(r.ActivityType));
        return await q.ToListAsync(ct);
    }

    public async Task DeleteAsync(List<Guid> ids, CancellationToken ct)
    {
        var records = await _ctx.ActivityRecords.Where(r => ids.Contains(r.Id)).ToListAsync(ct);
        _ctx.ActivityRecords.RemoveRange(records);
    }
}

public class AnalyticsReadStore : IAnalyticsReadStore
{
    private readonly ReportingDbContext _ctx;
    public AnalyticsReadStore(ReportingDbContext ctx) { _ctx = ctx; }

    public async Task UpsertAnalyticsRollupAsync(string metric, RollupGrain grain, DateTime bucketStart,
        string? industry, string? occupationCode, string? skillCode, string? region, Guid? employerId, decimal delta, CancellationToken ct)
    {
        var existing = await _ctx.AnalyticsRollups.FirstOrDefaultAsync(r =>
            r.Metric == metric && r.Grain == grain && r.BucketStartUtc == bucketStart &&
            r.Industry == industry && r.OccupationCode == occupationCode && r.SkillCode == skillCode &&
            r.Region == region && r.EmployerId == employerId, ct);

        if (existing is null)
        {
            await _ctx.AnalyticsRollups.AddAsync(new AnalyticsRollup
            {
                Id = Guid.NewGuid(), Metric = metric, Grain = grain, BucketStartUtc = bucketStart,
                Industry = industry, OccupationCode = occupationCode, SkillCode = skillCode,
                Region = region, EmployerId = employerId, Value = delta, SampleCount = 1, UpdatedOnUtc = DateTime.UtcNow
            }, ct);
        }
        else
        {
            existing.Value += delta;
            existing.SampleCount++;
            existing.UpdatedOnUtc = DateTime.UtcNow;
        }
    }

    public async Task UpsertSkillDemandAsync(string skillCode, string? industry, string? region, RollupGrain grain, DateTime bucketStart, int postingDelta, int supplyDelta, CancellationToken ct)
    {
        var existing = await _ctx.SkillDemandRollups.FirstOrDefaultAsync(r =>
            r.SkillCode == skillCode && r.Industry == industry && r.Region == region && r.Grain == grain && r.BucketStartUtc == bucketStart, ct);
        if (existing is null)
            await _ctx.SkillDemandRollups.AddAsync(new SkillDemandRollup { Id = Guid.NewGuid(), SkillCode = skillCode, Industry = industry, Region = region, Grain = grain, BucketStartUtc = bucketStart, PostingCount = postingDelta, CandidateSupplyCount = supplyDelta }, ct);
        else
        {
            existing.PostingCount += postingDelta;
            existing.CandidateSupplyCount += supplyDelta;
        }
    }

    public async Task UpsertOutcomeCohortAsync(RollupGrain grain, DateTime bucketStart, string? industry, string? region, string? skillCode, int appDelta, int offerDelta, int acceptDelta, CancellationToken ct)
    {
        var existing = await _ctx.OutcomeCohortRollups.FirstOrDefaultAsync(r =>
            r.Grain == grain && r.BucketStartUtc == bucketStart && r.Industry == industry && r.Region == region && r.SkillCode == skillCode, ct);
        if (existing is null)
            await _ctx.OutcomeCohortRollups.AddAsync(new OutcomeCohortRollup { Id = Guid.NewGuid(), Grain = grain, BucketStartUtc = bucketStart, Industry = industry, Region = region, SkillCode = skillCode, ApplicationCount = appDelta, OfferCount = offerDelta, AcceptanceCount = acceptDelta }, ct);
        else
        {
            existing.ApplicationCount += appDelta;
            existing.OfferCount += offerDelta;
            existing.AcceptanceCount += acceptDelta;
        }
    }

    public async Task UpsertMatchingMetricAsync(DateTime bucketStart, RollupGrain grain, int recommendationDelta, CancellationToken ct)
    {
        var existing = await _ctx.MatchingMetricRollups.FirstOrDefaultAsync(r => r.BucketStartUtc == bucketStart && r.Grain == grain, ct);
        if (existing is null)
            await _ctx.MatchingMetricRollups.AddAsync(new MatchingMetricRollup { Id = Guid.NewGuid(), BucketStartUtc = bucketStart, Grain = grain, RecommendationCount = recommendationDelta }, ct);
        else
            existing.RecommendationCount += recommendationDelta;
    }
}

public class PerformanceReadStore : IPerformanceReadStore
{
    private readonly ReportingDbContext _ctx;
    public PerformanceReadStore(ReportingDbContext ctx) { _ctx = ctx; }

    public async Task UpsertSystemMetricBucketAsync(string metricKey, DateTime bucketStart, int bucketSeconds, decimal value, CancellationToken ct)
    {
        var existing = await _ctx.SystemMetricBuckets.FirstOrDefaultAsync(b => b.MetricKey == metricKey && b.BucketStartUtc == bucketStart && b.BucketSeconds == bucketSeconds, ct);
        if (existing is null)
            await _ctx.SystemMetricBuckets.AddAsync(new SystemMetricBucket { Id = Guid.NewGuid(), MetricKey = metricKey, BucketStartUtc = bucketStart, BucketSeconds = bucketSeconds, Count = 1, Avg = value, Min = value, Max = value }, ct);
        else
        {
            existing.Count++;
            existing.Avg = (existing.Avg ?? 0) + (value - (existing.Avg ?? 0)) / existing.Count;
            if (existing.Min is null || value < existing.Min) existing.Min = value;
            if (existing.Max is null || value > existing.Max) existing.Max = value;
        }
    }

    public Task<List<SystemMetricBucket>> GetMetricHistoryAsync(string metricKey, DateTime from, DateTime to, CancellationToken ct)
        => _ctx.SystemMetricBuckets.Where(b => b.MetricKey == metricKey && b.BucketStartUtc >= from && b.BucketStartUtc <= to).OrderBy(b => b.BucketStartUtc).ToListAsync(ct);
}

public class ReportAccessLogStore : IReportAccessLogStore
{
    private readonly ReportingDbContext _ctx;
    public ReportAccessLogStore(ReportingDbContext ctx) { _ctx = ctx; }
    public async Task InsertAsync(ReportAccessLog log, CancellationToken ct) => await _ctx.ReportAccessLogs.AddAsync(log, ct);
    public Task<List<ReportAccessLog>> GetAuditAsync(Guid? userId, Guid? reportDefinitionId, int page, int pageSize, CancellationToken ct)
    {
        var q = _ctx.ReportAccessLogs.AsQueryable();
        if (userId.HasValue) q = q.Where(l => l.UserId == userId.Value);
        if (reportDefinitionId.HasValue) q = q.Where(l => l.ReportDefinitionId == reportDefinitionId.Value);
        return q.OrderByDescending(l => l.OccurredOnUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }
}

public class InboxStore : IInboxStore
{
    private readonly ReportingDbContext _ctx;
    public InboxStore(ReportingDbContext ctx) { _ctx = ctx; }
    public Task<bool> ExistsAsync(Guid eventId, CancellationToken ct)
        => _ctx.InboxMessages.AnyAsync(m => m.Id == eventId, ct);
    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct)
    {
        var msg = new InboxMessage(eventId, eventType, DateTime.UtcNow);
        msg.MarkProcessed(DateTime.UtcNow);
        await _ctx.InboxMessages.AddAsync(msg, ct);
    }
}
