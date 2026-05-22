using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.Aggregates;

public class SearchSessionTests
{
    private static readonly Guid SeekerId = Guid.NewGuid();
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public void Start_ShouldCreateSessionWithSliding24hExpiry()
    {
        var result = SearchSession.Start(SeekerId, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeekerUserId.Should().Be(SeekerId);
        result.Value.ExpiresOnUtc.Should().Be(Now.AddHours(24));
    }

    [Fact]
    public void RememberCriteria_ShouldStoreCriteriaAndExtendExpiry()
    {
        var session = SearchSession.Start(SeekerId, Now).Value;
        var criteria = SearchCriteria.Create(keyword: "developer").Value;
        var later = Now.AddHours(2);

        session.RememberCriteria(criteria, later);

        session.LastCriteria.Should().Be(criteria);
        session.ExpiresOnUtc.Should().Be(later.AddHours(24));
    }

    [Fact]
    public void DismissRecommendation_ShouldAddToDismissedSet()
    {
        var session = SearchSession.Start(SeekerId, Now).Value;
        var postingId = Guid.NewGuid();
        var later = Now.AddMinutes(30);

        session.DismissRecommendation(postingId, later);

        session.DismissedRecommendationPostingIds.Should().Contain(postingId);
    }

    [Fact]
    public void DismissRecommendation_ShouldDedupePostingId()
    {
        var session = SearchSession.Start(SeekerId, Now).Value;
        var postingId = Guid.NewGuid();

        session.DismissRecommendation(postingId, Now);
        session.DismissRecommendation(postingId, Now);

        session.DismissedRecommendationPostingIds.Should().HaveCount(1);
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_AfterExpiry()
    {
        var session = SearchSession.Start(SeekerId, Now).Value;
        var afterExpiry = Now.AddHours(25);

        session.IsExpired(afterExpiry).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_BeforeExpiry()
    {
        var session = SearchSession.Start(SeekerId, Now).Value;

        session.IsExpired(Now).Should().BeFalse();
    }
}
