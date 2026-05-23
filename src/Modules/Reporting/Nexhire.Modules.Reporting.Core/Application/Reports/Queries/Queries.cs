using Nexhire.Modules.Reporting.Core.Application.DTOs;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Application.Reports.Queries;

// --- Activity ---
public record GetUserActivityDashboardQuery : IQuery<UserActivityDashboardDto>;
public class GetUserActivityDashboardQueryHandler : IQueryHandler<GetUserActivityDashboardQuery, UserActivityDashboardDto>
{
    private readonly IActivityReadStore _store;
    public GetUserActivityDashboardQueryHandler(IActivityReadStore store) { _store = store; }
    public async Task<Result<UserActivityDashboardDto>> Handle(GetUserActivityDashboardQuery request, CancellationToken ct)
    {
        var sessions = await _store.GetActiveSessionsAsync(ct);
        var dtos = sessions.Select(s => new SessionSnapshotDto(s.UserId, s.DisplayName, s.ActorRole.ToString(), s.LastLoginOnUtc, s.LastSessionDurationSeconds, s.ConcurrentSessionCount, s.IsCurrentlyActive)).ToList();
        return Result.Success(new UserActivityDashboardDto(sessions.Count(s => s.IsCurrentlyActive), dtos));
    }
}

public record GetJobSeekerActivityReportQuery(DateTime? From, DateTime? To, int Page = 1, int PageSize = 50) : IQuery<ActivityReportDto>;
public class GetJobSeekerActivityReportQueryHandler : IQueryHandler<GetJobSeekerActivityReportQuery, ActivityReportDto>
{
    private readonly IActivityReadStore _store;
    public GetJobSeekerActivityReportQueryHandler(IActivityReadStore store) { _store = store; }
    public async Task<Result<ActivityReportDto>> Handle(GetJobSeekerActivityReportQuery request, CancellationToken ct)
    {
        var records = await _store.GetActivityRecordsAsync(null, ActorRole.JobSeeker, request.From, request.To, request.Page, request.PageSize, ct);
        var dtos = records.Select(r => new ActivityRecordDto(r.Id, r.UserId, r.ActorRole.ToString(), r.ActivityType.ToString(), r.OccurredOnUtc, r.TargetType, r.TargetId)).ToList();
        return Result.Success(new ActivityReportDto(dtos, dtos.Count, request.Page, request.PageSize));
    }
}

public record GetEmployerActivityReportQuery(DateTime? From, DateTime? To, int Page = 1, int PageSize = 50) : IQuery<ActivityReportDto>;
public class GetEmployerActivityReportQueryHandler : IQueryHandler<GetEmployerActivityReportQuery, ActivityReportDto>
{
    private readonly IActivityReadStore _store;
    public GetEmployerActivityReportQueryHandler(IActivityReadStore store) { _store = store; }
    public async Task<Result<ActivityReportDto>> Handle(GetEmployerActivityReportQuery request, CancellationToken ct)
    {
        var records = await _store.GetActivityRecordsAsync(null, ActorRole.Employer, request.From, request.To, request.Page, request.PageSize, ct);
        var dtos = records.Select(r => new ActivityRecordDto(r.Id, r.UserId, r.ActorRole.ToString(), r.ActivityType.ToString(), r.OccurredOnUtc, r.TargetType, r.TargetId)).ToList();
        return Result.Success(new ActivityReportDto(dtos, dtos.Count, request.Page, request.PageSize));
    }
}

public record GetActivityTimelineQuery(Guid UserId, DateTime? From, DateTime? To) : IQuery<List<ActivityRecordDto>>;
public class GetActivityTimelineQueryHandler : IQueryHandler<GetActivityTimelineQuery, List<ActivityRecordDto>>
{
    private readonly IActivityReadStore _store;
    public GetActivityTimelineQueryHandler(IActivityReadStore store) { _store = store; }
    public async Task<Result<List<ActivityRecordDto>>> Handle(GetActivityTimelineQuery request, CancellationToken ct)
    {
        var records = await _store.GetActivityRecordsAsync(request.UserId, null, request.From, request.To, 1, 200, ct);
        var dtos = records.Select(r => new ActivityRecordDto(r.Id, r.UserId, r.ActorRole.ToString(), r.ActivityType.ToString(), r.OccurredOnUtc, r.TargetType, r.TargetId)).ToList();
        return Result.Success(dtos);
    }
}

// --- Retention ---
public record GetRetentionPoliciesQuery : IQuery<List<RetentionPolicyDto>>;
public class GetRetentionPoliciesQueryHandler : IQueryHandler<GetRetentionPoliciesQuery, List<RetentionPolicyDto>>
{
    private readonly IRetentionPolicyRepository _repo;
    public GetRetentionPoliciesQueryHandler(IRetentionPolicyRepository repo) { _repo = repo; }
    public async Task<Result<List<RetentionPolicyDto>>> Handle(GetRetentionPoliciesQuery request, CancellationToken ct)
    {
        var policies = await _repo.GetActiveAsync(ct);
        return Result.Success(policies.Select(p => new RetentionPolicyDto(p.Id, p.Name, p.RetentionDays, p.Action.ToString(), p.WarningDays, p.Status.ToString(), p.EffectiveFromUtc)).ToList());
    }
}

// --- Reports ---
public record GetReportTemplateLibraryQuery(string CallerRole, ReportCategory? Category = null) : IQuery<List<ReportDefinitionSummaryDto>>;
public class GetReportTemplateLibraryQueryHandler : IQueryHandler<GetReportTemplateLibraryQuery, List<ReportDefinitionSummaryDto>>
{
    private readonly IReportDefinitionRepository _repo;
    public GetReportTemplateLibraryQueryHandler(IReportDefinitionRepository repo) { _repo = repo; }
    public async Task<Result<List<ReportDefinitionSummaryDto>>> Handle(GetReportTemplateLibraryQuery request, CancellationToken ct)
    {
        var defs = await _repo.ListActiveByCategoryAsync(request.Category, request.CallerRole, ct);
        var visible = defs.Where(d => d.Visibility.AllowsRole(request.CallerRole)).ToList();
        return Result.Success(visible.Select(d => new ReportDefinitionSummaryDto(d.Id, d.Name, d.Kind.ToString(), d.Category.ToString(), d.Status.ToString(), d.UsageCount, d.CreatedOnUtc)).ToList());
    }
}

public record GetReportRunQuery(Guid RunId) : IQuery<ReportRunDto>;
public class GetReportRunQueryHandler : IQueryHandler<GetReportRunQuery, ReportRunDto>
{
    private readonly IReportRunRepository _repo;
    public GetReportRunQueryHandler(IReportRunRepository repo) { _repo = repo; }
    public async Task<Result<ReportRunDto>> Handle(GetReportRunQuery request, CancellationToken ct)
    {
        var run = await _repo.GetByIdAsync(request.RunId, ct);
        if (run is null) return Result.Failure<ReportRunDto>(new Error("ReportRun.NotFound", "Not found."));
        return Result.Success(new ReportRunDto(run.Id, run.ReportDefinitionId, run.Status.ToString(), run.RowCount, run.FailureReason, run.QueuedOnUtc, run.CompletedOnUtc, run.Artifacts.Select(a => a.Format.ToString()).ToList()));
    }
}

public record GetReportSchedulesQuery : IQuery<List<ReportScheduleDto>>;
public class GetReportSchedulesQueryHandler : IQueryHandler<GetReportSchedulesQuery, List<ReportScheduleDto>>
{
    private readonly IReportScheduleRepository _repo;
    public GetReportSchedulesQueryHandler(IReportScheduleRepository repo) { _repo = repo; }
    public async Task<Result<List<ReportScheduleDto>>> Handle(GetReportSchedulesQuery request, CancellationToken ct)
    {
        var schedules = await _repo.ListAllAsync(ct);
        return Result.Success(schedules.Select(s => new ReportScheduleDto(s.Id, s.ReportDefinitionId, s.Status.ToString(), s.NextRunOnUtc, s.LastRunOnUtc, s.DistributionList.Select(e => e.Value).ToList())).ToList());
    }
}

// --- Alerts ---
public record GetAlertRulesQuery : IQuery<List<AlertRuleDto>>;
public class GetAlertRulesQueryHandler : IQueryHandler<GetAlertRulesQuery, List<AlertRuleDto>>
{
    private readonly IAlertRuleRepository _repo;
    public GetAlertRulesQueryHandler(IAlertRuleRepository repo) { _repo = repo; }
    public async Task<Result<List<AlertRuleDto>>> Handle(GetAlertRulesQuery request, CancellationToken ct)
    {
        var rules = await _repo.ListAllAsync(ct);
        return Result.Success(rules.Select(r => new AlertRuleDto(r.Id, r.Name, r.MetricKey, r.Severity.ToString(), r.Status.ToString(), r.AnomalyDetectionEnabled)).ToList());
    }
}

public record GetActiveAlertIncidentsQuery : IQuery<List<AlertIncidentDto>>;
public class GetActiveAlertIncidentsQueryHandler : IQueryHandler<GetActiveAlertIncidentsQuery, List<AlertIncidentDto>>
{
    private readonly IAlertRuleRepository _repo;
    public GetActiveAlertIncidentsQueryHandler(IAlertRuleRepository repo) { _repo = repo; }
    public async Task<Result<List<AlertIncidentDto>>> Handle(GetActiveAlertIncidentsQuery request, CancellationToken ct)
    {
        var rules = await _repo.ListAllAsync(ct);
        var incidents = rules.SelectMany(r => r.Incidents.Where(i => i.State == IncidentState.Raised)
            .Select(i => new AlertIncidentDto(i.Id, r.Id, i.ObservedValue, i.Trigger.ToString(), i.State.ToString(), i.TriggeredOnUtc))).ToList();
        return Result.Success(incidents);
    }
}

// --- Performance (stub queries returning empty data — real data comes from projectors) ---
public record GetSystemPerformanceDashboardQuery(DateTime From, DateTime To) : IQuery<SystemPerformanceDto>;
public class GetSystemPerformanceDashboardQueryHandler : IQueryHandler<GetSystemPerformanceDashboardQuery, SystemPerformanceDto>
{
    private readonly IPerformanceReadStore _store;
    public GetSystemPerformanceDashboardQueryHandler(IPerformanceReadStore store) { _store = store; }
    public async Task<Result<SystemPerformanceDto>> Handle(GetSystemPerformanceDashboardQuery request, CancellationToken ct)
    {
        var buckets = await _store.GetMetricHistoryAsync("api.latency.p95", request.From, request.To, ct);
        var dtos = buckets.Select(b => new MetricBucketDto(b.MetricKey, b.BucketStartUtc, b.P95, b.Avg, b.Count)).ToList();
        return Result.Success(new SystemPerformanceDto(dtos));
    }
}

public record GetEmploymentStatsDashboardQuery(DateTime From, DateTime To, RollupGrain Grain) : IQuery<EmploymentStatsDto>;
public class GetEmploymentStatsDashboardQueryHandler : IQueryHandler<GetEmploymentStatsDashboardQuery, EmploymentStatsDto>
{
    public async Task<Result<EmploymentStatsDto>> Handle(GetEmploymentStatsDashboardQuery request, CancellationToken ct)
        => Result.Success(new EmploymentStatsDto(new List<TimeSeriesPoint>(), new List<TimeSeriesPoint>(), new List<TimeSeriesPoint>()));
}

public record GetSkillDemandTrendsQuery(RollupGrain Grain) : IQuery<SkillDemandDto>;
public class GetSkillDemandTrendsQueryHandler : IQueryHandler<GetSkillDemandTrendsQuery, SkillDemandDto>
{
    public async Task<Result<SkillDemandDto>> Handle(GetSkillDemandTrendsQuery request, CancellationToken ct)
        => Result.Success(new SkillDemandDto(new List<SkillDemandItem>()));
}

public record GetMatchingPerformanceQuery(RollupGrain Grain) : IQuery<MatchingPerformanceDto>;
public class GetMatchingPerformanceQueryHandler : IQueryHandler<GetMatchingPerformanceQuery, MatchingPerformanceDto>
{
    public async Task<Result<MatchingPerformanceDto>> Handle(GetMatchingPerformanceQuery request, CancellationToken ct)
        => Result.Success(new MatchingPerformanceDto(null, null, null, 0, 0));
}

public record GetReportAccessAuditQuery(Guid? UserId, int Page = 1, int PageSize = 50) : IQuery<List<ReportAccessLogDto>>;
public class GetReportAccessAuditQueryHandler : IQueryHandler<GetReportAccessAuditQuery, List<ReportAccessLogDto>>
{
    private readonly IReportAccessLogStore _store;
    public GetReportAccessAuditQueryHandler(IReportAccessLogStore store) { _store = store; }
    public async Task<Result<List<ReportAccessLogDto>>> Handle(GetReportAccessAuditQuery request, CancellationToken ct)
    {
        var logs = await _store.GetAuditAsync(request.UserId, null, request.Page, request.PageSize, ct);
        return Result.Success(logs.Select(l => new ReportAccessLogDto(l.Id, l.UserId, l.Role, l.ReportDefinitionId, l.ReportRunId, l.Action, l.OccurredOnUtc)).ToList());
    }
}

public record GetAdminReportsMenuQuery(string CallerRole) : IQuery<AdminReportsMenuDto>;
public class GetAdminReportsMenuQueryHandler : IQueryHandler<GetAdminReportsMenuQuery, AdminReportsMenuDto>
{
    private readonly IReportDefinitionRepository _repo;
    public GetAdminReportsMenuQueryHandler(IReportDefinitionRepository repo) { _repo = repo; }
    public async Task<Result<AdminReportsMenuDto>> Handle(GetAdminReportsMenuQuery request, CancellationToken ct)
    {
        var defs = await _repo.ListActiveByCategoryAsync(null, request.CallerRole, ct);
        var visible = defs.Where(d => d.Visibility.AllowsRole(request.CallerRole))
            .Select(d => new ReportDefinitionSummaryDto(d.Id, d.Name, d.Kind.ToString(), d.Category.ToString(), d.Status.ToString(), d.UsageCount, d.CreatedOnUtc)).ToList();
        return Result.Success(new AdminReportsMenuDto(visible));
    }
}
