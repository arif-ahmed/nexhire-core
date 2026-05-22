using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Events;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.Aggregates;

public class FavoriteJobTests
{
    private static readonly Guid SeekerId = Guid.NewGuid();
    private static readonly Guid PostingId = Guid.NewGuid();
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public void Add_ShouldCreateFavorite_WhenValidIds()
    {
        var result = FavoriteJob.Add(SeekerId, PostingId, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeekerUserId.Should().Be(SeekerId);
        result.Value.PostingId.Should().Be(PostingId);
        result.Value.FavoritedOnUtc.Should().Be(Now);
    }

    [Fact]
    public void Add_ShouldRaiseJobFavoritedEvent()
    {
        var favorite = FavoriteJob.Add(SeekerId, PostingId, Now).Value;

        favorite.DomainEvents.Should().ContainSingle(e => e is JobFavorited);
    }

    [Fact]
    public void Add_ShouldFail_WhenSeekerIdEmpty()
    {
        var result = FavoriteJob.Add(Guid.Empty, PostingId, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FavoriteJob.EmptySeekerUserId");
    }

    [Fact]
    public void Add_ShouldFail_WhenPostingIdEmpty()
    {
        var result = FavoriteJob.Add(SeekerId, Guid.Empty, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FavoriteJob.EmptyPostingId");
    }

    [Fact]
    public void Remove_ShouldRaiseJobUnfavoritedEvent()
    {
        var favorite = FavoriteJob.Add(SeekerId, PostingId, Now).Value;
        favorite.ClearDomainEvents();

        var result = favorite.Remove();

        result.IsSuccess.Should().BeTrue();
        favorite.DomainEvents.Should().ContainSingle(e => e is JobUnfavorited);
    }
}
