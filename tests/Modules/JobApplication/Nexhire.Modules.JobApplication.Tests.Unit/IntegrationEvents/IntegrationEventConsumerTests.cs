using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using Nexhire.Modules.JobApplication.Infrastructure.IntegrationEvents;
using Nexhire.Modules.JobApplication.Infrastructure.IntegrationEvents.Consumers;
using Nexhire.Modules.JobApplication.Infrastructure.Persistence;
using Nexhire.Modules.JobApplication.Infrastructure.Persistence.Repositories;
using Nexhire.Shared.Infrastructure.Interceptors;
using Xunit;

namespace Nexhire.Modules.JobApplication.Tests.Unit.IntegrationEvents;

public class IntegrationEventConsumerTests
{
    private readonly JobApplicationDbContext _dbContext;
    private readonly IPublisher _publisherMock;
    private readonly ApplicationRepository _repository;
    private readonly JobApplicationUnitOfWork _unitOfWork;

    public IntegrationEventConsumerTests()
    {
        _publisherMock = Substitute.For<IPublisher>();
        var services = new ServiceCollection();
        services.AddSingleton(_publisherMock);
        var interceptor = new PublishDomainEventsInterceptor(services.BuildServiceProvider());

        var options = new DbContextOptionsBuilder<JobApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new JobApplicationDbContext(options, interceptor);
        _dbContext.Database.EnsureCreated();

        _repository = new ApplicationRepository(_dbContext);
        _unitOfWork = new JobApplicationUnitOfWork(_dbContext);
    }

    [Fact]
    public async Task JobPostingClosedConsumer_ShouldExpireOnlyActiveApplicationsForThatPosting()
    {
        // Arrange
        var targetPostingId = Guid.NewGuid();
        var otherPostingId = Guid.NewGuid();
        var seekerId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();

        var snapshot = CandidateSnapshot.Create(
            "Arif Ahmed", "arif@nexhire.com", "+12345", "Dhaka",
            "BSc", "Exp", new List<string> { "C#" }, true, DateTime.UtcNow).Value;
        var coverLetter = CoverLetter.Create("Cover Letter text").Value;

        // 1. Active Application on target posting
        var activeApp1 = Application.Submit(
            targetPostingId, seekerId, employerId, snapshot, resumeId, coverLetter, 90, Guid.NewGuid()).Value;

        // 2. Already Withdrawn (Terminal) Application on target posting
        var withdrawnApp = Application.Submit(
            targetPostingId, seekerId, employerId, snapshot, resumeId, coverLetter, 90, Guid.NewGuid()).Value;
        withdrawnApp.Withdraw(WithdrawalReason.Create("ChangedMind", "No interest").Value, seekerId);

        // 3. Active Application on another posting
        var activeAppOther = Application.Submit(
            otherPostingId, seekerId, employerId, snapshot, resumeId, coverLetter, 90, Guid.NewGuid()).Value;

        await _repository.AddAsync(activeApp1, CancellationToken.None);
        await _repository.AddAsync(withdrawnApp, CancellationToken.None);
        await _repository.AddAsync(activeAppOther, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        _dbContext.ChangeTracker.Clear();

        var consumer = new JobPostingClosedConsumer(_repository, _unitOfWork);
        var notificationEvent = new JobPostingClosedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, targetPostingId);

        // Act
        await consumer.Handle(notificationEvent, CancellationToken.None);

        // Assert
        var updatedActiveApp1 = await _repository.GetByIdAsync(activeApp1.Id, CancellationToken.None);
        var updatedWithdrawnApp = await _repository.GetByIdAsync(withdrawnApp.Id, CancellationToken.None);
        var updatedActiveAppOther = await _repository.GetByIdAsync(activeAppOther.Id, CancellationToken.None);

        updatedActiveApp1.Should().NotBeNull();
        updatedActiveApp1!.Status.Should().Be(ApplicationStatus.Expired);
        updatedActiveApp1.Stages.Should().Contain(s => s.Stage == ApplicationStatus.Expired && s.ReasonCode == "posting-closed" && s.EnteredByRole == StageActorRole.System);

        updatedWithdrawnApp.Should().NotBeNull();
        updatedWithdrawnApp!.Status.Should().Be(ApplicationStatus.Withdrawn); // Stays Withdrawn

        updatedActiveAppOther.Should().NotBeNull();
        updatedActiveAppOther!.Status.Should().Be(ApplicationStatus.Submitted); // Unaffected
    }

    [Fact]
    public async Task SeekerAccountDeactivatedConsumer_ShouldWithdrawOnlyActiveApplicationsForThatSeeker()
    {
        // Arrange
        var targetSeekerId = Guid.NewGuid();
        var otherSeekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();

        var snapshot = CandidateSnapshot.Create(
            "Arif Ahmed", "arif@nexhire.com", "+12345", "Dhaka",
            "BSc", "Exp", new List<string> { "C#" }, true, DateTime.UtcNow).Value;
        var coverLetter = CoverLetter.Create("Cover Letter text").Value;

        // 1. Active Application by target seeker
        var activeApp1 = Application.Submit(
            postingId, targetSeekerId, employerId, snapshot, resumeId, coverLetter, 90, Guid.NewGuid()).Value;

        // 2. Active Application by another seeker
        var activeAppOther = Application.Submit(
            postingId, otherSeekerId, employerId, snapshot, resumeId, coverLetter, 90, Guid.NewGuid()).Value;

        await _repository.AddAsync(activeApp1, CancellationToken.None);
        await _repository.AddAsync(activeAppOther, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        _dbContext.ChangeTracker.Clear();

        var consumer = new SeekerAccountDeactivatedConsumer(_repository, _unitOfWork);
        var notificationEvent = new AccountDeactivatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, targetSeekerId);

        // Act
        await consumer.Handle(notificationEvent, CancellationToken.None);

        // Assert
        var updatedActiveApp1 = await _repository.GetByIdAsync(activeApp1.Id, CancellationToken.None);
        var updatedActiveAppOther = await _repository.GetByIdAsync(activeAppOther.Id, CancellationToken.None);

        updatedActiveApp1.Should().NotBeNull();
        updatedActiveApp1!.Status.Should().Be(ApplicationStatus.Withdrawn);
        updatedActiveApp1.Stages.Should().Contain(s => s.Stage == ApplicationStatus.Withdrawn && s.ReasonCode == "AccountDeactivated" && s.EnteredByRole == StageActorRole.System);

        updatedActiveAppOther.Should().NotBeNull();
        updatedActiveAppOther!.Status.Should().Be(ApplicationStatus.Submitted); // Unaffected
    }
}
