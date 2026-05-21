using MediatR;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Infrastructure.IntegrationEvents;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.IntegrationEvents.Consumers;

public class UserAccountActivatedConsumer : INotificationHandler<UserAccountActivatedIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UserAccountActivatedConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UserAccountActivatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile != null)
        {
            var result = profile.Activate();
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class AccountDeactivatedConsumer : INotificationHandler<AccountDeactivatedIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AccountDeactivatedConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AccountDeactivatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile != null)
        {
            var result = profile.Deactivate();
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class UserAccountSuspendedConsumer : INotificationHandler<UserAccountSuspendedIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UserAccountSuspendedConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UserAccountSuspendedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile != null)
        {
            var result = profile.Suspend(notification.Reason);
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class UserAccountReinstatedConsumer : INotificationHandler<UserAccountReinstatedIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UserAccountReinstatedConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UserAccountReinstatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile != null)
        {
            var result = profile.Reinstate();
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class EmployerVerifiedByGovernmentConsumer : INotificationHandler<EmployerVerifiedByGovernmentIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public EmployerVerifiedByGovernmentConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EmployerVerifiedByGovernmentIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(notification.EmployerProfileId, cancellationToken);
        if (profile != null)
        {
            var result = profile.RecordAutomaticVerificationPassed(notification.EvidenceRef);
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class EmployerVerificationFailedByGovernmentConsumer : INotificationHandler<EmployerVerificationFailedByGovernmentIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public EmployerVerificationFailedByGovernmentConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EmployerVerificationFailedByGovernmentIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(notification.EmployerProfileId, cancellationToken);
        if (profile != null)
        {
            var result = profile.RecordAutomaticVerificationFailed();
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class JobPostingPublishedConsumer : INotificationHandler<JobPostingPublishedIntegrationEvent>
{
    private readonly IDashboardProjectionStore _store;

    public JobPostingPublishedConsumer(IDashboardProjectionStore store)
    {
        _store = store;
    }

    public async Task Handle(JobPostingPublishedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var posting = new DashboardPosting
        {
            PostingId = notification.PostingId,
            EmployerUserId = notification.EmployerUserId,
            Title = notification.Title,
            Status = "Active",
            LastEventOnUtc = notification.OccurredOnUtc
        };
        await _store.UpsertPostingAsync(posting, cancellationToken);
    }
}

public class JobPostingClosedConsumer : INotificationHandler<JobPostingClosedIntegrationEvent>
{
    private readonly IDashboardProjectionStore _store;

    public JobPostingClosedConsumer(IDashboardProjectionStore store)
    {
        _store = store;
    }

    public async Task Handle(JobPostingClosedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        await _store.RemovePostingAsync(notification.PostingId, cancellationToken);
    }
}

public class ApplicationSubmittedConsumer : INotificationHandler<ApplicationSubmittedIntegrationEvent>
{
    private readonly IDashboardProjectionStore _store;

    public ApplicationSubmittedConsumer(IDashboardProjectionStore store)
    {
        _store = store;
    }

    public async Task Handle(ApplicationSubmittedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var application = new DashboardApplication
        {
            ApplicationId = notification.ApplicationId,
            EmployerUserId = notification.EmployerUserId,
            PostingId = notification.PostingId,
            JobSeekerId = notification.JobSeekerId,
            SubmittedOnUtc = notification.OccurredOnUtc
        };
        await _store.AddApplicationAsync(application, cancellationToken);
    }
}

public class CandidateRecommendationGeneratedConsumer : INotificationHandler<CandidateRecommendationGeneratedIntegrationEvent>
{
    private readonly IDashboardProjectionStore _store;

    public CandidateRecommendationGeneratedConsumer(IDashboardProjectionStore store)
    {
        _store = store;
    }

    public async Task Handle(CandidateRecommendationGeneratedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var candidate = new DashboardMatchedCandidate
        {
            Id = notification.RecommendationId,
            EmployerUserId = notification.EmployerUserId,
            PostingId = notification.PostingId,
            CandidateUserId = notification.CandidateUserId,
            MatchScore = notification.MatchScore,
            GeneratedOnUtc = notification.OccurredOnUtc
        };
        await _store.UpsertMatchedCandidateAsync(candidate, cancellationToken);
    }
}
