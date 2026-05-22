using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Ports;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.SearchJobs;
using Nexhire.Shared.Core.Results;
using NSubstitute;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.Application;

public class SearchJobsTests
{
    private readonly IJobIndexEntryRepository _jobIndexRepo;
    private readonly ISearchSessionRepository _sessionRepo;
    private readonly IRecommendationQueryApi _recommendationApi;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SearchJobsQueryHandler _handler;

    public SearchJobsTests()
    {
        _jobIndexRepo = Substitute.For<IJobIndexEntryRepository>();
        _sessionRepo = Substitute.For<ISearchSessionRepository>();
        _recommendationApi = Substitute.For<IRecommendationQueryApi>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new SearchJobsQueryHandler(_jobIndexRepo, _sessionRepo, _recommendationApi, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldReturnResults_WhenMatchingJobsExist()
    {
        var entry = JobIndexEntry.Project(
            Guid.NewGuid(), Guid.NewGuid(), "Developer", "Summary", "Corp",
            [], null, null, "Dhaka", null, null, null,
            EmploymentType.FullTime, WorkFormat.Remote,
            null, null, null, null,
            DateTime.UtcNow, null, 1, DateTime.UtcNow).Value;

        _jobIndexRepo.SearchAsync(Arg.Any<Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects.SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _jobIndexRepo.CountAsync(Arg.Any<Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects.SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await _handler.Handle(new SearchJobsQuery("developer", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.NoResults.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoMatches()
    {
        _jobIndexRepo.SearchAsync(Arg.Any<Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects.SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _jobIndexRepo.CountAsync(Arg.Any<Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects.SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await _handler.Handle(new SearchJobsQuery("nonexistent", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NoResults.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenEmptyCriteria()
    {
        var result = await _handler.Handle(new SearchJobsQuery(null, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SearchCriteria.EmptyCriteria");
    }

    [Fact]
    public async Task Handle_Anonymous_ShouldNotIncludeRecommendations()
    {
        var entry = JobIndexEntry.Project(
            Guid.NewGuid(), Guid.NewGuid(), "Developer", "Summary", "Corp",
            [], null, null, "Dhaka", null, null, null,
            EmploymentType.FullTime, WorkFormat.Remote,
            null, null, null, null,
            DateTime.UtcNow, null, 1, DateTime.UtcNow).Value;

        _jobIndexRepo.SearchAsync(Arg.Any<Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects.SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _jobIndexRepo.CountAsync(Arg.Any<Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects.SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await _handler.Handle(new SearchJobsQuery("developer", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recommendations.Should().BeEmpty();
    }
}
