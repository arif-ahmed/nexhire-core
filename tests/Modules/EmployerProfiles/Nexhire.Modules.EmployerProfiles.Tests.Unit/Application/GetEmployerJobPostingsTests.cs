using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetEmployerJobPostings;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit.Application;

public class GetEmployerJobPostingsTests
{
    private readonly IDashboardProjectionStore _store = Substitute.For<IDashboardProjectionStore>();
    private readonly GetEmployerJobPostingsQueryHandler _handler;

    public GetEmployerJobPostingsTests()
    {
        _handler = new GetEmployerJobPostingsQueryHandler(_store);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoPostingsExist()
    {
        var userId = Guid.NewGuid();
        _store.GetPostingsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DashboardPosting>());

        var result = await _handler.Handle(new GetEmployerJobPostingsQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsPostings_FilteredToRequestingEmployer()
    {
        var userId = Guid.NewGuid();
        var postings = new[]
        {
            new DashboardPosting { PostingId = Guid.NewGuid(), EmployerUserId = userId, Title = "Dev Lead", Status = "Active", LastEventOnUtc = DateTime.UtcNow },
            new DashboardPosting { PostingId = Guid.NewGuid(), EmployerUserId = userId, Title = "QA Engineer", Status = "Active", LastEventOnUtc = DateTime.UtcNow }
        };
        _store.GetPostingsAsync(userId, Arg.Any<CancellationToken>()).Returns(postings);

        var result = await _handler.Handle(new GetEmployerJobPostingsQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(p => p.Title).Should().Contain("Dev Lead");
    }
}
