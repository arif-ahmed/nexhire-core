using MediatR;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Modules.SearchDiscovery.Infrastructure.IntegrationEvents;

namespace Nexhire.Modules.SearchDiscovery.Infrastructure.IntegrationEvents.Consumers;

public class JobPostingPublishedConsumer : INotificationHandler<JobPostingPublishedIntegrationEvent>
{
    private readonly IJobIndexEntryRepository _jobIndexRepo;
    private readonly IUnitOfWork _unitOfWork;

    public JobPostingPublishedConsumer(IJobIndexEntryRepository jobIndexRepo, IUnitOfWork unitOfWork)
    {
        _jobIndexRepo = jobIndexRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(JobPostingPublishedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var existing = await _jobIndexRepo.GetByIdAsync(notification.PostingId, cancellationToken);
        if (existing is not null)
            return;

        var result = JobIndexEntry.Project(
            notification.PostingId, notification.EmployerId,
            notification.Title, notification.Summary, notification.CompanyName,
            notification.Skills, notification.EducationRequirement, notification.ExperienceYears,
            notification.LocationDistrict, notification.LocationCity,
            notification.LocationLatitude, notification.LocationLongitude,
            Enum.Parse<EmploymentType>(notification.EmploymentType),
            Enum.Parse<WorkFormat>(notification.WorkFormat),
            notification.SalaryMin, notification.SalaryMax, notification.SalaryCurrency,
            notification.SectorIndustry,
            notification.PostedOnUtc, notification.ApplicationDeadlineUtc,
            notification.SourceVersion, DateTime.UtcNow);

        if (result.IsSuccess)
        {
            await _jobIndexRepo.AddAsync(result.Value, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

public class JobPostingUpdatedConsumer : INotificationHandler<JobPostingUpdatedIntegrationEvent>
{
    private readonly IJobIndexEntryRepository _jobIndexRepo;
    private readonly IUnitOfWork _unitOfWork;

    public JobPostingUpdatedConsumer(IJobIndexEntryRepository jobIndexRepo, IUnitOfWork unitOfWork)
    {
        _jobIndexRepo = jobIndexRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(JobPostingUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var entry = await _jobIndexRepo.GetByIdAsync(notification.PostingId, cancellationToken);
        if (entry is null)
            return;

        entry.ApplyUpdate(
            notification.Title, notification.Summary, notification.Skills,
            notification.EducationRequirement, notification.ExperienceYears,
            notification.LocationDistrict, notification.LocationCity,
            notification.LocationLatitude, notification.LocationLongitude,
            notification.EmploymentType is not null ? Enum.Parse<EmploymentType>(notification.EmploymentType) : null,
            notification.WorkFormat is not null ? Enum.Parse<WorkFormat>(notification.WorkFormat) : null,
            notification.SalaryMin, notification.SalaryMax, notification.SalaryCurrency,
            notification.SectorIndustry, notification.ApplicationDeadlineUtc,
            notification.SourceVersion);

        await _jobIndexRepo.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class JobPostingExpiredConsumer : INotificationHandler<JobPostingExpiredIntegrationEvent>
{
    private readonly IJobIndexEntryRepository _jobIndexRepo;
    private readonly IUnitOfWork _unitOfWork;

    public JobPostingExpiredConsumer(IJobIndexEntryRepository jobIndexRepo, IUnitOfWork unitOfWork)
    {
        _jobIndexRepo = jobIndexRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(JobPostingExpiredIntegrationEvent notification, CancellationToken cancellationToken)
    {
        await _jobIndexRepo.DeleteAsync(notification.PostingId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class JobPostingClosedConsumer : INotificationHandler<JobPostingClosedIntegrationEvent>
{
    private readonly IJobIndexEntryRepository _jobIndexRepo;
    private readonly IUnitOfWork _unitOfWork;

    public JobPostingClosedConsumer(IJobIndexEntryRepository jobIndexRepo, IUnitOfWork unitOfWork)
    {
        _jobIndexRepo = jobIndexRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(JobPostingClosedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        await _jobIndexRepo.DeleteAsync(notification.PostingId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class JobPostingSuspendedConsumer : INotificationHandler<JobPostingSuspendedIntegrationEvent>
{
    private readonly IJobIndexEntryRepository _jobIndexRepo;
    private readonly IUnitOfWork _unitOfWork;

    public JobPostingSuspendedConsumer(IJobIndexEntryRepository jobIndexRepo, IUnitOfWork unitOfWork)
    {
        _jobIndexRepo = jobIndexRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(JobPostingSuspendedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        await _jobIndexRepo.DeleteAsync(notification.PostingId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class JobPostingReinstatedConsumer : INotificationHandler<JobPostingReinstatedIntegrationEvent>
{
    private readonly IJobIndexEntryRepository _jobIndexRepo;
    private readonly IUnitOfWork _unitOfWork;

    public JobPostingReinstatedConsumer(IJobIndexEntryRepository jobIndexRepo, IUnitOfWork unitOfWork)
    {
        _jobIndexRepo = jobIndexRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(JobPostingReinstatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var result = JobIndexEntry.Project(
            notification.PostingId, notification.EmployerId,
            notification.Title, notification.Summary, notification.CompanyName,
            notification.Skills, notification.EducationRequirement, notification.ExperienceYears,
            notification.LocationDistrict, notification.LocationCity,
            notification.LocationLatitude, notification.LocationLongitude,
            Enum.Parse<EmploymentType>(notification.EmploymentType),
            Enum.Parse<WorkFormat>(notification.WorkFormat),
            notification.SalaryMin, notification.SalaryMax, notification.SalaryCurrency,
            notification.SectorIndustry,
            notification.PostedOnUtc, notification.ApplicationDeadlineUtc,
            notification.SourceVersion, DateTime.UtcNow);

        if (result.IsSuccess)
        {
            await _jobIndexRepo.AddAsync(result.Value, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

public class MatchComputedConsumer : INotificationHandler<MatchComputedIntegrationEvent>
{
    private readonly IMatchScoreCacheRepository _cacheRepo;
    private readonly IUnitOfWork _unitOfWork;

    public MatchComputedConsumer(IMatchScoreCacheRepository cacheRepo, IUnitOfWork unitOfWork)
    {
        _cacheRepo = cacheRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MatchComputedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        await _cacheRepo.UpsertAsync(notification.JobSeekerId, notification.PostingId, notification.Score, notification.OccurredOnUtc, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class RecommendationGeneratedConsumer : INotificationHandler<RecommendationGeneratedIntegrationEvent>
{
    private readonly IRecommendationCacheRepository _cacheRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RecommendationGeneratedConsumer(IRecommendationCacheRepository cacheRepo, IUnitOfWork unitOfWork)
    {
        _cacheRepo = cacheRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RecommendationGeneratedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        await _cacheRepo.ReplaceAsync(notification.JobSeekerId, notification.PostingIds.ToList(), notification.OccurredOnUtc, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class TaxonomyUpdatedConsumer : INotificationHandler<TaxonomyUpdatedIntegrationEvent>
{
    public Task Handle(TaxonomyUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
