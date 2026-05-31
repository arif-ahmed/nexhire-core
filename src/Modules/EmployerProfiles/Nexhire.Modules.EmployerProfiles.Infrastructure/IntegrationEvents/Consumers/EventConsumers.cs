using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Infrastructure.IntegrationEvents;
using Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.IntegrationEvents.Consumers;

public class UserAccountActivatedConsumer : INotificationHandler<UserAccountActivatedIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmployerProfilesDbContext _dbContext;

    public UserAccountActivatedConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork, EmployerProfilesDbContext dbContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task Handle(UserAccountActivatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _repository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile != null)
        {
            var result = profile.Activate();
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(UserAccountActivatedIntegrationEvent), DateTime.UtcNow));
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class AccountDeactivatedConsumer : INotificationHandler<AccountDeactivatedIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmployerProfilesDbContext _dbContext;

    public AccountDeactivatedConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork, EmployerProfilesDbContext dbContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task Handle(AccountDeactivatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _repository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile != null)
        {
            var result = profile.Deactivate();
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(AccountDeactivatedIntegrationEvent), DateTime.UtcNow));
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class UserAccountSuspendedConsumer : INotificationHandler<UserAccountSuspendedIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmployerProfilesDbContext _dbContext;

    public UserAccountSuspendedConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork, EmployerProfilesDbContext dbContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task Handle(UserAccountSuspendedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _repository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile != null)
        {
            var result = profile.Suspend(notification.Reason);
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(UserAccountSuspendedIntegrationEvent), DateTime.UtcNow));
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class UserAccountReinstatedConsumer : INotificationHandler<UserAccountReinstatedIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmployerProfilesDbContext _dbContext;

    public UserAccountReinstatedConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork, EmployerProfilesDbContext dbContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task Handle(UserAccountReinstatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _repository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile != null)
        {
            var result = profile.Reinstate();
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(UserAccountReinstatedIntegrationEvent), DateTime.UtcNow));
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class EmployerVerifiedByGovernmentConsumer : INotificationHandler<EmployerVerifiedByGovernmentIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmployerProfilesDbContext _dbContext;

    public EmployerVerifiedByGovernmentConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork, EmployerProfilesDbContext dbContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task Handle(EmployerVerifiedByGovernmentIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _repository.GetByIdAsync(notification.EmployerProfileId, cancellationToken);
        if (profile != null)
        {
            var result = profile.RecordAutomaticVerificationPassed(notification.EvidenceRef);
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(EmployerVerifiedByGovernmentIntegrationEvent), DateTime.UtcNow));
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class EmployerVerificationFailedByGovernmentConsumer : INotificationHandler<EmployerVerificationFailedByGovernmentIntegrationEvent>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmployerProfilesDbContext _dbContext;

    public EmployerVerificationFailedByGovernmentConsumer(IEmployerProfileRepository repository, IUnitOfWork unitOfWork, EmployerProfilesDbContext dbContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task Handle(EmployerVerificationFailedByGovernmentIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _repository.GetByIdAsync(notification.EmployerProfileId, cancellationToken);
        if (profile != null)
        {
            var result = profile.RecordAutomaticVerificationFailed();
            if (result.IsSuccess)
            {
                await _repository.UpdateAsync(profile, cancellationToken);
                _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(EmployerVerificationFailedByGovernmentIntegrationEvent), DateTime.UtcNow));
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

public class JobPostingPublishedConsumer : INotificationHandler<JobPostingPublishedIntegrationEvent>
{
    private readonly IDashboardProjectionStore _store;
    private readonly EmployerProfilesDbContext _dbContext;

    public JobPostingPublishedConsumer(IDashboardProjectionStore store, EmployerProfilesDbContext dbContext)
    {
        _store = store;
        _dbContext = dbContext;
    }

    public async Task Handle(JobPostingPublishedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var posting = new DashboardPosting
        {
            PostingId = notification.PostingId,
            EmployerUserId = notification.EmployerUserId,
            Title = notification.Title,
            Status = "Active",
            LastEventOnUtc = notification.OccurredOnUtc
        };
        await _store.UpsertPostingAsync(posting, cancellationToken);
        _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(JobPostingPublishedIntegrationEvent), DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class JobPostingClosedConsumer : INotificationHandler<JobPostingClosedIntegrationEvent>
{
    private readonly IDashboardProjectionStore _store;
    private readonly EmployerProfilesDbContext _dbContext;

    public JobPostingClosedConsumer(IDashboardProjectionStore store, EmployerProfilesDbContext dbContext)
    {
        _store = store;
        _dbContext = dbContext;
    }

    public async Task Handle(JobPostingClosedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        await _store.RemovePostingAsync(notification.PostingId, cancellationToken);
        _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(JobPostingClosedIntegrationEvent), DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class ApplicationSubmittedConsumer : INotificationHandler<ApplicationSubmittedIntegrationEvent>
{
    private readonly IDashboardProjectionStore _store;
    private readonly EmployerProfilesDbContext _dbContext;

    public ApplicationSubmittedConsumer(IDashboardProjectionStore store, EmployerProfilesDbContext dbContext)
    {
        _store = store;
        _dbContext = dbContext;
    }

    public async Task Handle(ApplicationSubmittedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var application = new DashboardApplication
        {
            ApplicationId = notification.ApplicationId,
            EmployerUserId = notification.EmployerUserId,
            PostingId = notification.PostingId,
            JobSeekerId = notification.JobSeekerId,
            SubmittedOnUtc = notification.OccurredOnUtc
        };
        await _store.AddApplicationAsync(application, cancellationToken);
        _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(ApplicationSubmittedIntegrationEvent), DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class CandidateRecommendationGeneratedConsumer : INotificationHandler<CandidateRecommendationGeneratedIntegrationEvent>
{
    private readonly IDashboardProjectionStore _store;
    private readonly EmployerProfilesDbContext _dbContext;

    public CandidateRecommendationGeneratedConsumer(IDashboardProjectionStore store, EmployerProfilesDbContext dbContext)
    {
        _store = store;
        _dbContext = dbContext;
    }

    public async Task Handle(CandidateRecommendationGeneratedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

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
        _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(CandidateRecommendationGeneratedIntegrationEvent), DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
