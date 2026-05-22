using MediatR;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.Ports;
using Nexhire.Modules.JobPostings.Core.Domain.Repositories;
using Nexhire.Modules.JobPostings.Core.Domain.Services;
using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Modules.JobPostings.Core.JobPostings;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Core.IntegrationEvents;

public sealed class EmployerStandingProjectionHandlers :
    INotificationHandler<EmployerVerifiedIntegrationEvent>,
    INotificationHandler<EmployerVerificationFailedIntegrationEvent>,
    INotificationHandler<EmployerAccountReinstatedIntegrationEvent>
{
    private readonly IEmployerStandingStore _standings;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public EmployerStandingProjectionHandlers(IEmployerStandingStore standings, IJobPostingsUnitOfWork unitOfWork)
    {
        _standings = standings;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EmployerVerifiedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var current = await _standings.GetAsync(notification.EmployerId, cancellationToken);
        await _standings.UpsertAsync(new EmployerStanding(notification.EmployerId, true, current?.IsActive ?? true, notification.VerifiedOnUtc), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(EmployerVerificationFailedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var current = await _standings.GetAsync(notification.EmployerId, cancellationToken);
        await _standings.UpsertAsync(new EmployerStanding(notification.EmployerId, false, current?.IsActive ?? true, notification.OccurredOnUtc), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(EmployerAccountReinstatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var current = await _standings.GetAsync(notification.EmployerId, cancellationToken);
        await _standings.UpsertAsync(new EmployerStanding(notification.EmployerId, current?.IsVerified ?? false, true, notification.OccurredOnUtc), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class EmployerAccountClosedHandlers :
    INotificationHandler<EmployerAccountDeactivatedIntegrationEvent>,
    INotificationHandler<EmployerAccountSuspendedIntegrationEvent>
{
    private readonly IEmployerStandingStore _standings;
    private readonly IJobPostingRepository _postings;
    private readonly IPostingAuditTrailRepository _auditTrails;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public EmployerAccountClosedHandlers(IEmployerStandingStore standings, IJobPostingRepository postings, IPostingAuditTrailRepository auditTrails, IJobPostingsUnitOfWork unitOfWork)
    {
        _standings = standings;
        _postings = postings;
        _auditTrails = auditTrails;
        _unitOfWork = unitOfWork;
    }

    public Task Handle(EmployerAccountDeactivatedIntegrationEvent notification, CancellationToken cancellationToken) =>
        CloseOpenPostings(notification.EmployerId, "employer-account-deactivated", notification.DeactivatedOnUtc, cancellationToken);

    public Task Handle(EmployerAccountSuspendedIntegrationEvent notification, CancellationToken cancellationToken) =>
        CloseOpenPostings(notification.EmployerId, notification.Reason, notification.OccurredOnUtc, cancellationToken);

    private async Task CloseOpenPostings(Guid employerId, string reason, DateTime occurredOnUtc, CancellationToken cancellationToken)
    {
        var current = await _standings.GetAsync(employerId, cancellationToken);
        await _standings.UpsertAsync(new EmployerStanding(employerId, current?.IsVerified ?? false, false, occurredOnUtc), cancellationToken);

        foreach (var posting in await _postings.GetOpenByEmployerIdAsync(employerId, cancellationToken))
        {
            var from = posting.Status;
            posting.CloseDueToEmployerStanding(reason, occurredOnUtc);
            var audit = await AuditTrailLoader.LoadAsync(posting.Id, _auditTrails, cancellationToken);
            audit.RecordStatusChange(AuditActor.System(), StatusTransition.Create(from, posting.Status).Value, reason, occurredOnUtc);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ExternalJobIngestedIntegrationEventHandler : INotificationHandler<ExternalJobIngestedIntegrationEvent>
{
    private readonly IJobPostingRepository _postings;
    private readonly IPostingAuditTrailRepository _auditTrails;
    private readonly ITaxonomyApi _taxonomyApi;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public ExternalJobIngestedIntegrationEventHandler(IJobPostingRepository postings, IPostingAuditTrailRepository auditTrails, ITaxonomyApi taxonomyApi, IJobPostingsUnitOfWork unitOfWork)
    {
        _postings = postings;
        _auditTrails = auditTrails;
        _taxonomyApi = taxonomyApi;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ExternalJobIngestedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _postings.GetByExternalRefAsync(notification.ExternalRef, cancellationToken) is not null)
        {
            return;
        }

        var details = await JobPostingDraftFactory.BuildAsync(notification.NormalizedPosting, _taxonomyApi, cancellationToken);
        if (details.IsFailure)
        {
            return;
        }

        var posting = JobPosting.CreateDraft(
            notification.EmployerId,
            notification.PostedByUserId,
            details.Value.Title,
            details.Value.Summary,
            details.Value.ContractType,
            details.Value.EducationLevel,
            details.Value.WorkFormat,
            details.Value.Location,
            details.Value.RequiredSkills,
            details.Value.RequiredLanguages,
            details.Value.Deadline,
            details.Value.JobLink,
            details.Value.SalaryRange,
            details.Value.Visibility,
            notification.OccurredOnUtc);

        if (posting.IsFailure)
        {
            return;
        }

        posting.Value.MarkExternalMirror(notification.ExternalRef);
        await _postings.AddAsync(posting.Value, cancellationToken);
        await _auditTrails.AddAsync(PostingAuditTrail.Create(posting.Value.Id), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class TaxonomyUpdatedIntegrationEventHandler : INotificationHandler<TaxonomyUpdatedIntegrationEvent>
{
    private readonly IJobPostingRepository _postings;
    private readonly IPostingAuditTrailRepository _auditTrails;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public TaxonomyUpdatedIntegrationEventHandler(IJobPostingRepository postings, IPostingAuditTrailRepository auditTrails, IJobPostingsUnitOfWork unitOfWork)
    {
        _postings = postings;
        _auditTrails = auditTrails;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(TaxonomyUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        foreach (var posting in await _postings.GetBySkillCodesAsync(notification.DeprecatedSkillCodes, cancellationToken))
        {
            var matched = posting.FlagDeprecatedSkillCodes(notification.DeprecatedSkillCodes, notification.OccurredOnUtc);
            if (matched.Count == 0)
            {
                continue;
            }

            var audit = await AuditTrailLoader.LoadAsync(posting.Id, _auditTrails, cancellationToken);
            audit.RecordFieldEdit(AuditActor.System(), matched.Select(code => $"DeprecatedSkill:{code}").ToArray(), notification.OccurredOnUtc);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class PostingMetricsProjectionHandlers :
    INotificationHandler<JobPostingApplicationsCountChangedIntegrationEvent>,
    INotificationHandler<JobPostingMatchesCountChangedIntegrationEvent>,
    INotificationHandler<JobPostingViewsCountChangedIntegrationEvent>
{
    private readonly IPostingMetricsStore _metrics;
    private readonly IJobPostingsUnitOfWork _unitOfWork;

    public PostingMetricsProjectionHandlers(IPostingMetricsStore metrics, IJobPostingsUnitOfWork unitOfWork)
    {
        _metrics = metrics;
        _unitOfWork = unitOfWork;
    }

    public Task Handle(JobPostingApplicationsCountChangedIntegrationEvent notification, CancellationToken cancellationToken) =>
        Upsert(notification.JobPostingId, notification.OccurredOnUtc, current => current.WithApplications(notification.ApplicationsCount, notification.OccurredOnUtc), cancellationToken);

    public Task Handle(JobPostingMatchesCountChangedIntegrationEvent notification, CancellationToken cancellationToken) =>
        Upsert(notification.JobPostingId, notification.OccurredOnUtc, current => current.WithMatches(notification.MatchesCount, notification.OccurredOnUtc), cancellationToken);

    public Task Handle(JobPostingViewsCountChangedIntegrationEvent notification, CancellationToken cancellationToken) =>
        Upsert(notification.JobPostingId, notification.OccurredOnUtc, current => current.WithViews(notification.ViewsCount, notification.OccurredOnUtc), cancellationToken);

    private async Task Upsert(Guid postingId, DateTime occurredOnUtc, Func<PostingMetrics, PostingMetrics> update, CancellationToken cancellationToken)
    {
        var current = await _metrics.GetAsync(postingId, cancellationToken) ?? new PostingMetrics(postingId, 0, 0, 0, occurredOnUtc);
        await _metrics.UpsertAsync(update(current), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
