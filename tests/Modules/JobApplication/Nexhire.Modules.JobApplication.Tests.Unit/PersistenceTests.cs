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
using Nexhire.Modules.JobApplication.Infrastructure.Persistence;
using Nexhire.Modules.JobApplication.Infrastructure.Persistence.Repositories;
using Nexhire.Shared.Infrastructure.Interceptors;
using Xunit;
using ApplicationId = Nexhire.Modules.JobApplication.Core.Domain.ApplicationId;

namespace Nexhire.Modules.JobApplication.Tests.Unit;

public class PersistenceTests
{
    private readonly JobApplicationDbContext _dbContext;
    private readonly IPublisher _publisherMock;

    public PersistenceTests()
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
    }

    [Fact]
    public async Task SaveAndRetrieveApplication_ShouldMapAllFieldsAndCollectionsCorrectly()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();
        var key = Guid.NewGuid();

        var skills = new List<string> { "C#", "DDD", "Clean Architecture" };
        var candidateSnapshot = CandidateSnapshot.Create(
            "Arif Ahmed",
            "arif@nexhire.com",
            "+123456789",
            "Dhaka",
            "BSc in Computer Science",
            "5 years of experience",
            skills,
            true,
            DateTime.UtcNow).Value;

        var coverLetter = CoverLetter.Create("Highly motivated applicant.").Value;

        var application = Application.Submit(
            postingId,
            seekerId,
            employerId,
            candidateSnapshot,
            resumeId,
            coverLetter,
            95,
            key).Value;

        var repository = new ApplicationRepository(_dbContext);
        var unitOfWork = new JobApplicationUnitOfWork(_dbContext);

        // Act
        await repository.AddAsync(application, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        _dbContext.ChangeTracker.Clear();

        var retrieved = await repository.GetByIdAsync(application.Id, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(application.Id);
        retrieved.JobPostingId.Should().Be(postingId);
        retrieved.JobSeekerId.Should().Be(seekerId);
        retrieved.EmployerId.Should().Be(employerId);
        retrieved.ResumeDocumentId.Should().Be(resumeId);
        retrieved.MatchScoreAtApply.Should().Be(95);
        retrieved.IdempotencyKey.Should().Be(key);
        retrieved.CoverLetter.Should().NotBeNull();
        retrieved.CoverLetter!.Text.Should().Be("Highly motivated applicant.");
        
        // Candidate Snapshot assertions
        retrieved.CandidateSnapshot.Should().NotBeNull();
        retrieved.CandidateSnapshot.FullName.Should().Be("Arif Ahmed");
        retrieved.CandidateSnapshot.Email.Should().Be("arif@nexhire.com");
        retrieved.CandidateSnapshot.Skills.Should().BeEquivalentTo(skills);
        retrieved.CandidateSnapshot.IsLevel2Complete.Should().BeTrue();

        // Stages assertions
        retrieved.Stages.Should().HaveCount(1);
        var stage = retrieved.Stages.First();
        stage.Stage.Should().Be(ApplicationStatus.Submitted);
        stage.EnteredByRole.Should().Be(StageActorRole.Seeker);
        stage.Comment.Should().Be("Application formally submitted.");
    }

    [Fact]
    public async Task SaveAndRetrieveBookmark_ShouldSucceed()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();

        var bookmark = Bookmark.Create(seekerId, postingId);
        var repository = new BookmarkRepository(_dbContext);
        var unitOfWork = new JobApplicationUnitOfWork(_dbContext);

        // Act
        await repository.AddAsync(bookmark, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        _dbContext.ChangeTracker.Clear();

        var retrieved = await repository.GetAsync(seekerId, postingId, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(bookmark.Id);
        retrieved.JobSeekerId.Should().Be(seekerId);
        retrieved.JobPostingId.Should().Be(postingId);
    }

    [Fact]
    public async Task IdempotencyKeyStore_ShouldSaveAndRetrieveKeys()
    {
        // Arrange
        var store = new IdempotencyKeyStore(_dbContext);
        var unitOfWork = new JobApplicationUnitOfWork(_dbContext);
        var key = Guid.NewGuid();
        var appId = ApplicationId.New();

        // Act
        await store.SaveAsync(key, appId, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        _dbContext.ChangeTracker.Clear();

        var retrievedAppId = await store.TryGetAsync(key, CancellationToken.None);

        // Assert
        retrievedAppId.Should().NotBeNull();
        retrievedAppId.Should().Be(appId.Value);
    }

    [Fact]
    public async Task SeededWithdrawalReasons_ShouldBePresent()
    {
        // Act & Assert
        var reasons = await _dbContext.WithdrawalReasons.ToListAsync();
        reasons.Should().NotBeEmpty();
        reasons.Select(x => x.Code).Should().Contain(new[]
        {
            "ChangedMind",
            "AcceptedAnotherOffer",
            "NoLongerInterested",
            "RoleNotAsExpected",
            "AccountDeactivated"
        });
    }

    [Fact]
    public async Task OptimisticConcurrency_ShouldThrowException_OnConcurrentUpdate()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();

        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<JobApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var svc2 = new ServiceCollection();
        svc2.AddSingleton(_publisherMock);
        var interceptor = new PublishDomainEventsInterceptor(svc2.BuildServiceProvider());

        var candidateSnapshot = CandidateSnapshot.Create(
            "Arif Ahmed", "arif@nexhire.com", "+12345", "Dhaka",
            "BSc", "Exp", null, true, DateTime.UtcNow).Value;

        var application = Application.Submit(
            postingId, seekerId, employerId, candidateSnapshot, resumeId, null, null, Guid.NewGuid()).Value;

        // Seed database
        using (var seedContext = new JobApplicationDbContext(options, interceptor))
        {
            seedContext.Database.EnsureCreated();
            var repo = new ApplicationRepository(seedContext);
            await repo.AddAsync(application, CancellationToken.None);
            await seedContext.SaveChangesAsync();
        }

        // Load concurrent Transaction 1
        using var context1 = new JobApplicationDbContext(options, interceptor);
        var repo1 = new ApplicationRepository(context1);
        var uow1 = new JobApplicationUnitOfWork(context1);
        var instance1 = await repo1.GetByIdAsync(application.Id, CancellationToken.None);

        // Load concurrent Transaction 2
        using var context2 = new JobApplicationDbContext(options, interceptor);
        var repo2 = new ApplicationRepository(context2);
        var uow2 = new JobApplicationUnitOfWork(context2);
        var instance2 = await repo2.GetByIdAsync(application.Id, CancellationToken.None);

        instance1.Should().NotBeNull();
        instance2.Should().NotBeNull();

        // Transaction 1 updates the application status
        instance1!.Withdraw(WithdrawalReason.Create("ChangedMind", "Decided to stay").Value, seekerId);
        await uow1.SaveChangesAsync(CancellationToken.None);

        // Transaction 2 tries to update status with the stale instance (which still has old Version)
        instance2!.Withdraw(WithdrawalReason.Create("AcceptedAnotherOffer", "Got a better deal").Value, seekerId);

        // Act & Assert
        Func<Task> act = async () => await uow2.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
