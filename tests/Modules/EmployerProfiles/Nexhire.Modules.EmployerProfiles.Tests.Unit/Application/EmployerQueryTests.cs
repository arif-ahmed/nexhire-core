using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetMyEmployerProfile;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetEmployerVerificationStatus;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetPublicEmployerProfile;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetEmployerDashboard;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetMatchedCandidates;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetShortlists;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetShortlist;
using Nexhire.Shared.Core.Results;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit.Application;

public class EmployerQueryTests
{
    private readonly IEmployerProfileRepository _employerRepository = Substitute.For<IEmployerProfileRepository>();
    private readonly IShortlistRepository _shortlistRepository = Substitute.For<IShortlistRepository>();
    private readonly IDashboardProjectionStore _projectionStore = Substitute.For<IDashboardProjectionStore>();

    private (Guid UserId, EmployerProfile Profile) CreateActiveProfile()
    {
        var userId = Guid.NewGuid();
        var profile = EmployerProfile.Register(
            Guid.NewGuid(),
            userId,
            CompanyName.Create("Nexhire Inc.").Value,
            EmailAddress.Create("info@nexhire.com").Value,
            MobileNumber.Create("+8801712345678").Value,
            CompanyIdentifier.Create("REG123456").Value);
        profile.Activate();
        return (userId, profile);
    }

    [Fact]
    public async Task GetMyEmployerProfile_ShouldSucceed_WhenProfileExists()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var handler = new GetMyEmployerProfileQueryHandler(_employerRepository);
        var query = new GetMyEmployerProfileQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyName.Should().Be("Nexhire Inc.");
        result.Value.Email.Should().Be("info@nexhire.com");
    }

    [Fact]
    public async Task GetEmployerVerificationStatus_ShouldSucceed_WhenProfileExists()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var handler = new GetEmployerVerificationStatusQueryHandler(_employerRepository);
        var query = new GetEmployerVerificationStatusQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be("NotStarted");
    }

    [Fact]
    public async Task GetPublicEmployerProfile_ShouldSucceed_WhenProfileExists()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);

        var handler = new GetPublicEmployerProfileQueryHandler(_employerRepository);
        var query = new GetPublicEmployerProfileQuery(profile.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyName.Should().Be("Nexhire Inc.");
    }

    [Fact]
    public async Task GetEmployerDashboard_ShouldSucceed_WhenProfileExists()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var postings = new List<DashboardPosting>
        {
            new() { PostingId = Guid.NewGuid(), EmployerUserId = userId, Title = "Software Engineer", Status = "Active" }
        };
        var applications = new List<DashboardApplication>
        {
            new() { ApplicationId = Guid.NewGuid(), EmployerUserId = userId, JobSeekerId = Guid.NewGuid(), PostingId = postings[0].PostingId }
        };
        var matches = new List<DashboardMatchedCandidate>
        {
            new() { Id = Guid.NewGuid(), EmployerUserId = userId, CandidateUserId = Guid.NewGuid(), PostingId = postings[0].PostingId, MatchScore = 95 }
        };

        _projectionStore.GetPostingsAsync(userId, Arg.Any<CancellationToken>()).Returns(postings);
        _projectionStore.GetApplicationsAsync(userId, Arg.Any<CancellationToken>()).Returns(applications);
        _projectionStore.GetMatchedCandidatesAsync(userId, Arg.Any<CancellationToken>()).Returns(matches);

        var handler = new GetEmployerDashboardQueryHandler(_employerRepository, _projectionStore);
        var query = new GetEmployerDashboardQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ActivePostingsCount.Should().Be(1);
        result.Value.TotalApplicationsCount.Should().Be(1);
        result.Value.TotalMatchesCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMatchedCandidates_ShouldSucceed_WhenCandidatesExist()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var matches = new List<DashboardMatchedCandidate>
        {
            new() { Id = Guid.NewGuid(), EmployerUserId = userId, CandidateUserId = Guid.NewGuid(), PostingId = Guid.NewGuid(), MatchScore = 95 }
        };
        _projectionStore.GetMatchedCandidatesAsync(userId, Arg.Any<CancellationToken>()).Returns(matches);

        var handler = new GetMatchedCandidatesQueryHandler(_employerRepository, _projectionStore);
        var query = new GetMatchedCandidatesQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().MatchScore.Should().Be(95);
    }

    [Fact]
    public async Task GetShortlists_ShouldSucceed_WhenShortlistsExist()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var shortlists = new List<Shortlist>
        {
            Shortlist.Create(Guid.NewGuid(), profile.Id, "Senior Devs").Value
        };
        _shortlistRepository.GetByEmployerProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(shortlists);

        var handler = new GetShortlistsQueryHandler(_employerRepository, _shortlistRepository);
        var query = new GetShortlistsQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Name.Should().Be("Senior Devs");
    }

    [Fact]
    public async Task GetShortlist_ShouldSucceed_WhenShortlistExists()
    {
        // Arrange
        var (userId, profile) = CreateActiveProfile();
        _employerRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var shortlist = Shortlist.Create(Guid.NewGuid(), profile.Id, "Senior Devs").Value;
        shortlist.AddCandidate(Guid.NewGuid(), 88);

        _shortlistRepository.GetByIdAsync(shortlist.Id, Arg.Any<CancellationToken>())
            .Returns(shortlist);

        var handler = new GetShortlistQueryHandler(_employerRepository, _shortlistRepository);
        var query = new GetShortlistQuery(userId, shortlist.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Senior Devs");
        result.Value.Members.Should().HaveCount(1);
        result.Value.Members.First().MatchScore.Should().Be(88);
    }
}
