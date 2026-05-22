using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;
using Nexhire.Modules.JobApplication.Infrastructure.IntegrationEvents;

namespace Nexhire.Modules.JobApplication.Infrastructure.IntegrationEvents.Consumers;

public class JobPostingClosedConsumer : INotificationHandler<JobPostingClosedIntegrationEvent>
{
    private readonly IApplicationRepository _repository;
    private readonly IJobApplicationUnitOfWork _unitOfWork;

    public JobPostingClosedConsumer(IApplicationRepository repository, IJobApplicationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(JobPostingClosedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var applications = await _repository.GetNonTerminalByPostingAsync(notification.PostingId, cancellationToken);
        if (applications.Count == 0) return;

        foreach (var application in applications)
        {
            application.MarkExpiredDueToPostingClosure("posting-closed");
            _repository.Update(application);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class SeekerAccountDeactivatedConsumer : INotificationHandler<AccountDeactivatedIntegrationEvent>
{
    private readonly IApplicationRepository _repository;
    private readonly IJobApplicationUnitOfWork _unitOfWork;

    public SeekerAccountDeactivatedConsumer(IApplicationRepository repository, IJobApplicationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AccountDeactivatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var applications = await _repository.GetNonTerminalBySeekerAsync(notification.UserId, cancellationToken);
        if (applications.Count == 0) return;

        foreach (var application in applications)
        {
            application.WithdrawDueToAccountDeactivation();
            _repository.Update(application);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
