using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Nexhire.Modules.JobApplication.Core.Domain;
using Nexhire.Modules.JobApplication.Core.Domain.Ports;
using Nexhire.Modules.JobApplication.Core.Domain.Repositories;
using Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;
using Nexhire.Modules.JobApplication.Core.DTOs;
using Nexhire.Modules.JobApplication.Core.JobApplications.Commands;
using Nexhire.Modules.JobApplication.Core.JobApplications.Queries;
using Nexhire.Shared.Core.Results;
using Nexhire.Shared.Core.Domain;
using Xunit;
using ApplicationId = Nexhire.Modules.JobApplication.Core.Domain.ApplicationId;
using BookmarkId = Nexhire.Modules.JobApplication.Core.Domain.BookmarkId;

namespace Nexhire.Modules.JobApplication.Tests.Unit.CQRS;

public class JobApplicationHandlerTests
{
    private readonly IApplicationRepository _applicationRepository = Substitute.For<IApplicationRepository>();
    private readonly IBookmarkRepository _bookmarkRepository = Substitute.For<IBookmarkRepository>();
    private readonly IJobPostingApi _jobPostingApi = Substitute.For<IJobPostingApi>();
    private readonly IJobSeekerProfileApi _jobSeekerProfileApi = Substitute.For<IJobSeekerProfileApi>();
    private readonly IMatchRankingPublicApi _matchRankingPublicApi = Substitute.For<IMatchRankingPublicApi>();
    private readonly IIdempotencyKeyStore _idempotencyKeyStore = Substitute.For<IIdempotencyKeyStore>();
    private readonly IJobApplicationUnitOfWork _unitOfWork = Substitute.For<IJobApplicationUnitOfWork>();

    // ----------------------------------------------------
    // ADD / REMOVE BOOKMARK COMMAND TESTS
    // ----------------------------------------------------

    [Fact]
    public async Task AddBookmark_Should_Succeed_WhenBookmarkDoesNotExist()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var command = new AddBookmarkCommand(seekerId, postingId);

        _bookmarkRepository.GetAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns((Bookmark?)null);

        var handler = new AddBookmarkCommandHandler(_bookmarkRepository, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _bookmarkRepository.Received(1).AddAsync(Arg.Any<Bookmark>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddBookmark_Should_Fail_WhenBookmarkAlreadyExists()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var command = new AddBookmarkCommand(seekerId, postingId);
        var bookmark = Bookmark.Create(seekerId, postingId);

        _bookmarkRepository.GetAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns(bookmark);

        var handler = new AddBookmarkCommandHandler(_bookmarkRepository, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-BOOKMARK-DUPLICATE");
        await _bookmarkRepository.DidNotReceive().AddAsync(Arg.Any<Bookmark>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveBookmark_Should_Succeed_WhenBookmarkExists()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var command = new RemoveBookmarkCommand(seekerId, postingId);
        var bookmark = Bookmark.Create(seekerId, postingId);

        _bookmarkRepository.GetAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns(bookmark);

        var handler = new RemoveBookmarkCommandHandler(_bookmarkRepository, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _bookmarkRepository.Received(1).Remove(bookmark);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveBookmark_Should_Fail_WhenBookmarkDoesNotExist()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var command = new RemoveBookmarkCommand(seekerId, postingId);

        _bookmarkRepository.GetAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns((Bookmark?)null);

        var handler = new RemoveBookmarkCommandHandler(_bookmarkRepository, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-BOOKMARK-NOT-FOUND");
        _bookmarkRepository.DidNotReceive().Remove(Arg.Any<Bookmark>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ----------------------------------------------------
    // SUBMIT APPLICATION COMMAND TESTS
    // ----------------------------------------------------

    [Fact]
    public async Task SubmitApplication_Should_Succeed_WhenEligible()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();
        var key = Guid.NewGuid();
        var command = new SubmitApplicationCommand(seekerId, postingId, resumeId, "Cover Letter Text", null, key);

        _idempotencyKeyStore.TryGetAsync(key, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Active", DateTime.UtcNow.AddDays(5));
        _jobPostingApi.GetApplicabilityAsync(postingId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(posting));

        _jobSeekerProfileApi.IsLevel2CompleteAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(true);

        _jobSeekerProfileApi.IsResumeUsableAsync(seekerId, resumeId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        _applicationRepository.GetNonTerminalForAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns((Nexhire.Modules.JobApplication.Core.Domain.Application?)null);

        var profile = new JobSeekerProfileSnapshotDto(
            seekerId, Guid.NewGuid(), "Arif Ahmed", "arif@nexhire.com", "+123456",
            "Dhaka", "BSc in CSE", "5 Years", new List<string> { "C#", ".NET" }, true, "Public");

        _jobSeekerProfileApi.GetSnapshotAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(profile));

        _matchRankingPublicApi.GetMatchScoreAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns(Result.Success<int?>(85));

        _applicationRepository.GetTerminalForAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Nexhire.Modules.JobApplication.Core.Domain.Application>());

        var handler = new SubmitApplicationCommandHandler(
            _applicationRepository, _jobPostingApi, _jobSeekerProfileApi,
            _matchRankingPublicApi, _idempotencyKeyStore, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Submitted");
        result.Value.MatchScoreAtApply.Should().Be(85);

        await _applicationRepository.Received(1).AddAsync(Arg.Any<Nexhire.Modules.JobApplication.Core.Domain.Application>(), Arg.Any<CancellationToken>());
        await _idempotencyKeyStore.Received(1).SaveAsync(key, Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitApplication_Should_ReturnExistingApplication_WhenIdempotencyKeyAlreadySeen()
    {
        // Arrange
        var key = Guid.NewGuid();
        var existingAppId = Guid.NewGuid();
        var command = new SubmitApplicationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, key);

        _idempotencyKeyStore.TryGetAsync(key, Arg.Any<CancellationToken>())
            .Returns(existingAppId);

        var candidateSnapshot = CandidateSnapshot.Create(
            "Arif Ahmed", "arif@nexhire.com", "+123456", "Dhaka",
            "BSc", "Exp", new[] { "C#" }, true, DateTime.UtcNow).Value;

        var application = Nexhire.Modules.JobApplication.Core.Domain.Application.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            candidateSnapshot, Guid.NewGuid(), null, 90, key, null).Value;

        _applicationRepository.GetByIdAsync(Arg.Is<ApplicationId>(id => id.Value == existingAppId), Arg.Any<CancellationToken>())
            .Returns(application);

        var handler = new SubmitApplicationCommandHandler(
            _applicationRepository, _jobPostingApi, _jobSeekerProfileApi,
            _matchRankingPublicApi, _idempotencyKeyStore, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApplicationId.Should().Be(application.Id.Value);
        result.Value.Status.Should().Be(application.Status.ToString());
        result.Value.MatchScoreAtApply.Should().Be(application.MatchScoreAtApply);

        await _applicationRepository.DidNotReceive().AddAsync(Arg.Any<Nexhire.Modules.JobApplication.Core.Domain.Application>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitApplication_Should_Fail_WhenPostingClosed()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();
        var key = Guid.NewGuid();
        var command = new SubmitApplicationCommand(seekerId, postingId, resumeId, null, null, key);

        _idempotencyKeyStore.TryGetAsync(key, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Closed", null);
        _jobPostingApi.GetApplicabilityAsync(postingId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(posting));

        _jobSeekerProfileApi.IsLevel2CompleteAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(true);

        _jobSeekerProfileApi.IsResumeUsableAsync(seekerId, resumeId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var handler = new SubmitApplicationCommandHandler(
            _applicationRepository, _jobPostingApi, _jobSeekerProfileApi,
            _matchRankingPublicApi, _idempotencyKeyStore, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-POSTING-CLOSED");
    }

    [Fact]
    public async Task SubmitApplication_Should_Fail_WhenLevel2CompleteIsFalse()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();
        var key = Guid.NewGuid();
        var command = new SubmitApplicationCommand(seekerId, postingId, resumeId, null, null, key);

        _idempotencyKeyStore.TryGetAsync(key, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Active", null);
        _jobPostingApi.GetApplicabilityAsync(postingId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(posting));

        _jobSeekerProfileApi.IsLevel2CompleteAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(false);

        _jobSeekerProfileApi.IsResumeUsableAsync(seekerId, resumeId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var handler = new SubmitApplicationCommandHandler(
            _applicationRepository, _jobPostingApi, _jobSeekerProfileApi,
            _matchRankingPublicApi, _idempotencyKeyStore, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-PROFILE-INCOMPLETE");
    }

    [Fact]
    public async Task SubmitApplication_Should_Fail_WhenResumeNotUsable()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();
        var key = Guid.NewGuid();
        var command = new SubmitApplicationCommand(seekerId, postingId, resumeId, null, null, key);

        _idempotencyKeyStore.TryGetAsync(key, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Active", null);
        _jobPostingApi.GetApplicabilityAsync(postingId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(posting));

        _jobSeekerProfileApi.IsLevel2CompleteAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(true);

        _jobSeekerProfileApi.IsResumeUsableAsync(seekerId, resumeId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(false)); // Not usable

        var handler = new SubmitApplicationCommandHandler(
            _applicationRepository, _jobPostingApi, _jobSeekerProfileApi,
            _matchRankingPublicApi, _idempotencyKeyStore, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-RESUME-MISSING");
    }

    [Fact]
    public async Task SubmitApplication_Should_Fail_WhenActiveApplicationAlreadyExists()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();
        var key = Guid.NewGuid();
        var command = new SubmitApplicationCommand(seekerId, postingId, resumeId, null, null, key);

        _idempotencyKeyStore.TryGetAsync(key, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Active", null);
        _jobPostingApi.GetApplicabilityAsync(postingId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(posting));

        _jobSeekerProfileApi.IsLevel2CompleteAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(true);

        _jobSeekerProfileApi.IsResumeUsableAsync(seekerId, resumeId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var candidateSnapshot = CandidateSnapshot.Create(
            "Arif", "arif@nexhire.com", "+123", "Dhaka",
            "BSc", "Exp", new[] { "C#" }, true, DateTime.UtcNow).Value;

        var existingApp = Nexhire.Modules.JobApplication.Core.Domain.Application.Submit(
            postingId, seekerId, Guid.NewGuid(),
            candidateSnapshot, resumeId, null, 90, Guid.NewGuid(), null).Value;

        _applicationRepository.GetNonTerminalForAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns(existingApp);

        var handler = new SubmitApplicationCommandHandler(
            _applicationRepository, _jobPostingApi, _jobSeekerProfileApi,
            _matchRankingPublicApi, _idempotencyKeyStore, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-DUPLICATE");
    }

    [Fact]
    public async Task SubmitApplication_Should_SucceedAndSetScoreToNull_WhenMatchScoreApiThrows()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();
        var key = Guid.NewGuid();
        var command = new SubmitApplicationCommand(seekerId, postingId, resumeId, null, null, key);

        _idempotencyKeyStore.TryGetAsync(key, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Active", null);
        _jobPostingApi.GetApplicabilityAsync(postingId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(posting));

        _jobSeekerProfileApi.IsLevel2CompleteAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(true);

        _jobSeekerProfileApi.IsResumeUsableAsync(seekerId, resumeId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        _applicationRepository.GetNonTerminalForAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns((Nexhire.Modules.JobApplication.Core.Domain.Application?)null);

        var profile = new JobSeekerProfileSnapshotDto(
            seekerId, Guid.NewGuid(), "Arif Ahmed", "arif@nexhire.com", "+123456",
            "Dhaka", "BSc in CSE", "5 Years", new List<string> { "C#" }, true, "Public");

        _jobSeekerProfileApi.GetSnapshotAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(profile));

        _matchRankingPublicApi.GetMatchScoreAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Recommendation engine offline."));

        _applicationRepository.GetTerminalForAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Nexhire.Modules.JobApplication.Core.Domain.Application>());

        var handler = new SubmitApplicationCommandHandler(
            _applicationRepository, _jobPostingApi, _jobSeekerProfileApi,
            _matchRankingPublicApi, _idempotencyKeyStore, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MatchScoreAtApply.Should().BeNull();
    }

    [Fact]
    public async Task SubmitApplication_Should_PopulateReplacesApplicationId_WhenTerminalPriorApplicationExists()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();
        var key = Guid.NewGuid();
        var command = new SubmitApplicationCommand(seekerId, postingId, resumeId, null, null, key);

        _idempotencyKeyStore.TryGetAsync(key, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var posting = new PostingApplicabilitySnapshot(postingId, Guid.NewGuid(), "Active", null);
        _jobPostingApi.GetApplicabilityAsync(postingId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(posting));

        _jobSeekerProfileApi.IsLevel2CompleteAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(true);

        _jobSeekerProfileApi.IsResumeUsableAsync(seekerId, resumeId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        _applicationRepository.GetNonTerminalForAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns((Nexhire.Modules.JobApplication.Core.Domain.Application?)null);

        var profile = new JobSeekerProfileSnapshotDto(
            seekerId, Guid.NewGuid(), "Arif Ahmed", "arif@nexhire.com", "+123456",
            "Dhaka", "BSc in CSE", "5 Years", new List<string> { "C#" }, true, "Public");

        _jobSeekerProfileApi.GetSnapshotAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(profile));

        var candidateSnapshot = CandidateSnapshot.Create(
            "Arif", "arif@nexhire.com", "+123", "Dhaka",
            "BSc", "Exp", new[] { "C#" }, true, DateTime.UtcNow).Value;

        var priorTerminalApp = Nexhire.Modules.JobApplication.Core.Domain.Application.Submit(
            postingId, seekerId, Guid.NewGuid(),
            candidateSnapshot, resumeId, null, 90, Guid.NewGuid(), null).Value;

        // Force it to a terminal state (Withdrawn) legally:
        var reason = WithdrawalReason.Create("ChangedMind", "Changed my mind").Value;
        priorTerminalApp.Withdraw(reason, seekerId);

        _applicationRepository.GetTerminalForAsync(seekerId, postingId, Arg.Any<CancellationToken>())
            .Returns(new[] { priorTerminalApp });

        var handler = new SubmitApplicationCommandHandler(
            _applicationRepository, _jobPostingApi, _jobSeekerProfileApi,
            _matchRankingPublicApi, _idempotencyKeyStore, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _applicationRepository.Received(1).AddAsync(
            Arg.Is<Nexhire.Modules.JobApplication.Core.Domain.Application>(a => a.ReplacesApplicationId == priorTerminalApp.Id),
            Arg.Any<CancellationToken>());
    }

    // ----------------------------------------------------
    // WITHDRAW APPLICATION COMMAND TESTS
    // ----------------------------------------------------

    [Fact]
    public async Task WithdrawApplication_Should_Succeed_WhenApplicationIsActiveAndOwnedBySeeker()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var command = new WithdrawApplicationCommand(appId, seekerId, "ChangedMind", "No longer looking");

        var candidateSnapshot = CandidateSnapshot.Create(
            "Arif", "arif@nexhire.com", "+123", "Dhaka",
            "BSc", "Exp", new[] { "C#" }, true, DateTime.UtcNow).Value;

        var application = Nexhire.Modules.JobApplication.Core.Domain.Application.Submit(
            Guid.NewGuid(), seekerId, Guid.NewGuid(),
            candidateSnapshot, Guid.NewGuid(), null, 90, Guid.NewGuid(), null).Value;

        // Set aggregate ID
        var idField = typeof(Entity<ApplicationId>).GetProperty("Id");
        idField?.SetValue(application, new ApplicationId(appId));

        _applicationRepository.GetByIdAsync(Arg.Is<ApplicationId>(id => id.Value == appId), Arg.Any<CancellationToken>())
            .Returns(application);

        var handler = new WithdrawApplicationCommandHandler(_applicationRepository, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Withdrawn");
        application.Status.Should().Be(ApplicationStatus.Withdrawn);

        _applicationRepository.Received(1).Update(application);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawApplication_Should_Fail_WhenApplicationNotOwnedBySeeker()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var otherSeekerId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var command = new WithdrawApplicationCommand(appId, seekerId, "ChangedMind", null);

        var candidateSnapshot = CandidateSnapshot.Create(
            "Arif", "arif@nexhire.com", "+123", "Dhaka",
            "BSc", "Exp", new[] { "C#" }, true, DateTime.UtcNow).Value;

        var application = Nexhire.Modules.JobApplication.Core.Domain.Application.Submit(
            Guid.NewGuid(), otherSeekerId, Guid.NewGuid(),
            candidateSnapshot, Guid.NewGuid(), null, 90, Guid.NewGuid(), null).Value;

        var idField = typeof(Entity<ApplicationId>).GetProperty("Id");
        idField?.SetValue(application, new ApplicationId(appId));

        _applicationRepository.GetByIdAsync(Arg.Is<ApplicationId>(id => id.Value == appId), Arg.Any<CancellationToken>())
            .Returns(application);

        var handler = new WithdrawApplicationCommandHandler(_applicationRepository, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-FORBIDDEN");
        application.Status.Should().Be(ApplicationStatus.Submitted); // unchanged
    }

    // ----------------------------------------------------
    // SEEKER QUERIES TESTS
    // ----------------------------------------------------

    [Fact]
    public async Task GetMyBookmarks_Should_JoinPostingSummaries()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var query = new GetMyBookmarksQuery(seekerId);

        var bookmark = Bookmark.Create(seekerId, postingId);
        _bookmarkRepository.ListBySeekerAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(new[] { bookmark });

        var summary = new PostingSummaryDto(postingId, "Software Engineer", "Nexhire", "Dhaka", "$1000", "Active");
        _jobPostingApi.GetSummariesAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(postingId)), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<PostingSummaryDto>>(new[] { summary }));

        var handler = new GetMyBookmarksQueryHandler(_bookmarkRepository, _jobPostingApi);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var dto = result.Value.First();
        dto.JobPostingId.Should().Be(postingId);
        dto.Title.Should().Be("Software Engineer");
        dto.CompanyName.Should().Be("Nexhire");
    }

    [Fact]
    public async Task GetMyApplications_Should_FilterAndPage()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var query = new GetMyApplicationsQuery(seekerId, Status: "Submitted", Page: 1, PageSize: 5);

        var candidateSnapshot = CandidateSnapshot.Create(
            "Arif", "arif@nexhire.com", "+123", "Dhaka",
            "BSc", "Exp", new[] { "C#" }, true, DateTime.UtcNow).Value;

        var postingId1 = Guid.NewGuid();
        var app1 = Nexhire.Modules.JobApplication.Core.Domain.Application.Submit(
            postingId1, seekerId, Guid.NewGuid(),
            candidateSnapshot, Guid.NewGuid(), null, null, Guid.NewGuid(), null).Value;

        _applicationRepository.ListBySeekerAsync(seekerId, Arg.Any<CancellationToken>())
            .Returns(new[] { app1 });

        var summary = new PostingSummaryDto(postingId1, "Product Manager", "Nexhire", "Remote", null, "Active");
        _jobPostingApi.GetSummariesAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(postingId1)), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<PostingSummaryDto>>(new[] { summary }));

        var handler = new GetMyApplicationsQueryHandler(_applicationRepository, _jobPostingApi);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        var dto = result.Value.Items.First();
        dto.JobPostingId.Should().Be(postingId1);
        dto.Title.Should().Be("Product Manager");
    }

    [Fact]
    public async Task GetMyApplicationDetail_Should_EnforceOwnership()
    {
        // Arrange
        var seekerId = Guid.NewGuid();
        var otherSeekerId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var query = new GetMyApplicationDetailQuery(appId, seekerId);

        var candidateSnapshot = CandidateSnapshot.Create(
            "Arif", "arif@nexhire.com", "+123", "Dhaka",
            "BSc", "Exp", new[] { "C#" }, true, DateTime.UtcNow).Value;

        var application = Nexhire.Modules.JobApplication.Core.Domain.Application.Submit(
            Guid.NewGuid(), otherSeekerId, Guid.NewGuid(),
            candidateSnapshot, Guid.NewGuid(), null, null, Guid.NewGuid(), null).Value;

        var idField = typeof(Entity<ApplicationId>).GetProperty("Id");
        idField?.SetValue(application, new ApplicationId(appId));

        _applicationRepository.GetByIdAsync(Arg.Is<ApplicationId>(id => id.Value == appId), Arg.Any<CancellationToken>())
            .Returns(application);

        var handler = new GetMyApplicationDetailQueryHandler(_applicationRepository, _jobPostingApi);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-APP-FORBIDDEN");
    }
}
