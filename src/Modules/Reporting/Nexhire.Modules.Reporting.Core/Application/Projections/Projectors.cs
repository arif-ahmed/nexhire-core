using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.ReadModels;
using Nexhire.Modules.Reporting.Core.Domain.Repositories;
using Nexhire.Modules.Reporting.Core.Domain.Services;

namespace Nexhire.Modules.Reporting.Core.Application.Projections;

// Base integration event for projectors
public abstract class IntegrationEventBase
{
    public Guid EventId { get; init; }
    public DateTime OccurredOnUtc { get; init; }
    public string EventType => GetType().Name;
}

// Representative integration events consumed
public class UserLoggedInIntegrationEvent : IntegrationEventBase
{
    public Guid UserId { get; init; }
    public string? DisplayName { get; init; }
}

public class UserRegisteredIntegrationEvent : IntegrationEventBase
{
    public Guid UserId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public class JobPostingPublishedIntegrationEvent : IntegrationEventBase
{
    public Guid PostingId { get; init; }
    public Guid EmployerId { get; init; }
    public string? Industry { get; init; }
    public string? OccupationCode { get; init; }
    public string? Region { get; init; }
    public List<string> RequiredSkills { get; init; } = new();
    public decimal? SalaryMin { get; init; }
    public decimal? SalaryMax { get; init; }
}

public class ApplicationSubmittedIntegrationEvent : IntegrationEventBase
{
    public Guid ApplicationId { get; init; }
    public Guid JobSeekerId { get; init; }
    public Guid PostingId { get; init; }
    public string? Industry { get; init; }
    public string? Region { get; init; }
}

public class ApplicationStatusChangedIntegrationEvent : IntegrationEventBase
{
    public Guid ApplicationId { get; init; }
    public Guid? EmployerId { get; init; }
    public string FromStatus { get; init; } = string.Empty;
    public string ToStatus { get; init; } = string.Empty;
    public string? Industry { get; init; }
    public string? Region { get; init; }
}

public class SystemMetricSampledIntegrationEvent : IntegrationEventBase
{
    public string MetricKey { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public string Unit { get; init; } = string.Empty;
}

public class MatchComputedIntegrationEvent : IntegrationEventBase
{
    public Guid JobSeekerId { get; init; }
    public Guid PostingId { get; init; }
    public decimal Score { get; init; }
}

public class SearchPerformedIntegrationEvent : IntegrationEventBase
{
    public Guid UserId { get; init; }
    public string? ActorRoleHint { get; init; }
    public string? Query { get; init; }
    public int ResultCount { get; init; }
}

// Projector service — handles idempotent projection
public class ProjectorService
{
    private readonly IActivityReadStore _activityStore;
    private readonly IAnalyticsReadStore _analyticsStore;
    private readonly IPerformanceReadStore _performanceStore;
    private readonly IInboxStore _inbox;

    public ProjectorService(IActivityReadStore activityStore, IAnalyticsReadStore analyticsStore,
        IPerformanceReadStore performanceStore, IInboxStore inbox)
    {
        _activityStore = activityStore;
        _analyticsStore = analyticsStore;
        _performanceStore = performanceStore;
        _inbox = inbox;
    }

    public async Task ProjectAsync(IntegrationEventBase evt, CancellationToken ct = default)
    {
        if (await _inbox.ExistsAsync(evt.EventId, ct)) return;

        await HandleAsync(evt, ct);
        await _inbox.MarkProcessedAsync(evt.EventId, evt.EventType, ct);
    }

    private async Task HandleAsync(IntegrationEventBase evt, CancellationToken ct)
    {
        switch (evt)
        {
            case UserLoggedInIntegrationEvent e:
                await ProjectUserLoggedIn(e, ct);
                break;
            case UserRegisteredIntegrationEvent e:
                await ProjectUserRegistered(e, ct);
                break;
            case JobPostingPublishedIntegrationEvent e:
                await ProjectJobPostingPublished(e, ct);
                break;
            case ApplicationSubmittedIntegrationEvent e:
                await ProjectApplicationSubmitted(e, ct);
                break;
            case ApplicationStatusChangedIntegrationEvent e:
                await ProjectApplicationStatusChanged(e, ct);
                break;
            case SystemMetricSampledIntegrationEvent e:
                await ProjectSystemMetricSampled(e, ct);
                break;
            case MatchComputedIntegrationEvent e:
                await ProjectMatchComputed(e, ct);
                break;
            case SearchPerformedIntegrationEvent e:
                await ProjectSearchPerformed(e, ct);
                break;
            default:
                await ProjectGenericActivity(evt, ct);
                break;
        }
    }

    private async Task ProjectUserLoggedIn(UserLoggedInIntegrationEvent e, CancellationToken ct)
    {
        if (!await _activityStore.ActivityRecordExistsAsync(e.EventId, ActivityType.Login, ct))
        {
            await _activityStore.InsertActivityRecordAsync(new ActivityRecord
            {
                Id = Guid.NewGuid(), UserId = e.UserId, ActorRole = ActorRole.JobSeeker,
                ActivityType = ActivityType.Login, OccurredOnUtc = e.OccurredOnUtc,
                SourceEventId = e.EventId, ProjectedOnUtc = DateTime.UtcNow
            }, ct);
        }

        await _activityStore.UpsertSessionSnapshotAsync(new SessionSnapshot
        {
            UserId = e.UserId, DisplayName = e.DisplayName ?? e.UserId.ToString(),
            ActorRole = ActorRole.JobSeeker, LastLoginOnUtc = e.OccurredOnUtc,
            ConcurrentSessionCount = 1, IsCurrentlyActive = true, UpdatedOnUtc = DateTime.UtcNow
        }, ct);
    }

    private async Task ProjectUserRegistered(UserRegisteredIntegrationEvent e, CancellationToken ct)
    {
        if (!await _activityStore.ActivityRecordExistsAsync(e.EventId, ActivityType.UserRegistered, ct))
        {
            await _activityStore.InsertActivityRecordAsync(new ActivityRecord
            {
                Id = Guid.NewGuid(), UserId = e.UserId, ActorRole = ActorRole.JobSeeker,
                ActivityType = ActivityType.UserRegistered, OccurredOnUtc = e.OccurredOnUtc,
                SourceEventId = e.EventId, ProjectedOnUtc = DateTime.UtcNow
            }, ct);
        }

        foreach (var grain in new[] { RollupGrain.Day, RollupGrain.Week, RollupGrain.Month })
            await _analyticsStore.UpsertAnalyticsRollupAsync("registration.count", grain, BucketStart(e.OccurredOnUtc, grain), null, null, null, null, null, 1, ct);
    }

    private async Task ProjectJobPostingPublished(JobPostingPublishedIntegrationEvent e, CancellationToken ct)
    {
        if (!await _activityStore.ActivityRecordExistsAsync(e.EventId, ActivityType.JobPostingPublished, ct))
        {
            await _activityStore.InsertActivityRecordAsync(new ActivityRecord
            {
                Id = Guid.NewGuid(), UserId = e.EmployerId, ActorRole = ActorRole.Employer,
                ActivityType = ActivityType.JobPostingPublished, OccurredOnUtc = e.OccurredOnUtc,
                TargetType = "JobPosting", TargetId = e.PostingId, SourceEventId = e.EventId,
                ProjectedOnUtc = DateTime.UtcNow
            }, ct);
        }

        foreach (var grain in new[] { RollupGrain.Day, RollupGrain.Week, RollupGrain.Month })
        {
            await _analyticsStore.UpsertAnalyticsRollupAsync("posting.volume", grain, BucketStart(e.OccurredOnUtc, grain),
                e.Industry, e.OccupationCode, null, e.Region, null, 1, ct);
        }

        foreach (var skill in e.RequiredSkills)
        {
            foreach (var grain in new[] { RollupGrain.Day, RollupGrain.Week, RollupGrain.Month })
                await _analyticsStore.UpsertSkillDemandAsync(skill, e.Industry, e.Region, grain, BucketStart(e.OccurredOnUtc, grain), 1, 0, ct);
        }
    }

    private async Task ProjectApplicationSubmitted(ApplicationSubmittedIntegrationEvent e, CancellationToken ct)
    {
        if (!await _activityStore.ActivityRecordExistsAsync(e.EventId, ActivityType.JobApplication, ct))
        {
            await _activityStore.InsertActivityRecordAsync(new ActivityRecord
            {
                Id = Guid.NewGuid(), UserId = e.JobSeekerId, ActorRole = ActorRole.JobSeeker,
                ActivityType = ActivityType.JobApplication, OccurredOnUtc = e.OccurredOnUtc,
                TargetType = "JobPosting", TargetId = e.PostingId, SourceEventId = e.EventId,
                ProjectedOnUtc = DateTime.UtcNow
            }, ct);
        }

        foreach (var grain in new[] { RollupGrain.Day, RollupGrain.Week, RollupGrain.Month })
        {
            await _analyticsStore.UpsertAnalyticsRollupAsync("application.count", grain, BucketStart(e.OccurredOnUtc, grain), e.Industry, null, null, e.Region, null, 1, ct);
            await _analyticsStore.UpsertOutcomeCohortAsync(grain, BucketStart(e.OccurredOnUtc, grain), e.Industry, e.Region, null, 1, 0, 0, ct);
        }
    }

    private async Task ProjectApplicationStatusChanged(ApplicationStatusChangedIntegrationEvent e, CancellationToken ct)
    {
        if (e.EmployerId.HasValue && !await _activityStore.ActivityRecordExistsAsync(e.EventId, ActivityType.ApplicationStatusChanged, ct))
        {
            await _activityStore.InsertActivityRecordAsync(new ActivityRecord
            {
                Id = Guid.NewGuid(), UserId = e.EmployerId!.Value, ActorRole = ActorRole.Employer,
                ActivityType = ActivityType.ApplicationStatusChanged, OccurredOnUtc = e.OccurredOnUtc,
                TargetType = "Application", TargetId = e.ApplicationId, SourceEventId = e.EventId,
                ProjectedOnUtc = DateTime.UtcNow
            }, ct);
        }

        if (e.ToStatus is "Offered" or "Hired")
        {
            var offerDelta = e.ToStatus == "Offered" ? 1 : 0;
            var acceptDelta = e.ToStatus == "Hired" ? 1 : 0;
            foreach (var grain in new[] { RollupGrain.Day, RollupGrain.Week, RollupGrain.Month })
            {
                await _analyticsStore.UpsertOutcomeCohortAsync(grain, BucketStart(e.OccurredOnUtc, grain), e.Industry, e.Region, null, 0, offerDelta, acceptDelta, ct);
                if (e.ToStatus == "Hired")
                    await _analyticsStore.UpsertAnalyticsRollupAsync("hire.count", grain, BucketStart(e.OccurredOnUtc, grain), e.Industry, null, null, e.Region, null, 1, ct);
            }
        }
    }

    private async Task ProjectSystemMetricSampled(SystemMetricSampledIntegrationEvent e, CancellationToken ct)
    {
        await _performanceStore.UpsertSystemMetricBucketAsync(e.MetricKey, BucketStart(e.OccurredOnUtc, RollupGrain.Day), 60, e.Value, ct);
    }

    private async Task ProjectMatchComputed(MatchComputedIntegrationEvent e, CancellationToken ct)
    {
        var bucketStart = new DateTime(e.OccurredOnUtc.Year, e.OccurredOnUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        await _analyticsStore.UpsertMatchingMetricAsync(bucketStart, RollupGrain.Month, 1, ct);
    }

    private async Task ProjectSearchPerformed(SearchPerformedIntegrationEvent e, CancellationToken ct)
    {
        var classification = ActivityClassifier.Classify("SearchPerformedIntegrationEvent", e.ActorRoleHint);
        if (classification.IsActivity && !await _activityStore.ActivityRecordExistsAsync(e.EventId, classification.ActivityType!.Value, ct))
        {
            await _activityStore.InsertActivityRecordAsync(new ActivityRecord
            {
                Id = Guid.NewGuid(), UserId = e.UserId, ActorRole = classification.ActorRole!.Value,
                ActivityType = classification.ActivityType!.Value, OccurredOnUtc = e.OccurredOnUtc,
                Metadata = $"{{\"query\":\"{e.Query}\",\"resultCount\":{e.ResultCount}}}",
                SourceEventId = e.EventId, ProjectedOnUtc = DateTime.UtcNow
            }, ct);
        }
    }

    private async Task ProjectGenericActivity(IntegrationEventBase evt, CancellationToken ct)
    {
        var classification = ActivityClassifier.Classify(evt.EventType);
        if (!classification.IsActivity) return;

        if (!await _activityStore.ActivityRecordExistsAsync(evt.EventId, classification.ActivityType!.Value, ct))
        {
            await _activityStore.InsertActivityRecordAsync(new ActivityRecord
            {
                Id = Guid.NewGuid(), UserId = Guid.Empty, ActorRole = classification.ActorRole!.Value,
                ActivityType = classification.ActivityType!.Value, OccurredOnUtc = evt.OccurredOnUtc,
                SourceEventId = evt.EventId, ProjectedOnUtc = DateTime.UtcNow
            }, ct);
        }
    }

    private static DateTime BucketStart(DateTime dt, RollupGrain grain) => grain switch
    {
        RollupGrain.Day => dt.Date,
        RollupGrain.Week => dt.Date.AddDays(-(int)dt.DayOfWeek),
        RollupGrain.Month => new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc),
        _ => dt.Date
    };
}
