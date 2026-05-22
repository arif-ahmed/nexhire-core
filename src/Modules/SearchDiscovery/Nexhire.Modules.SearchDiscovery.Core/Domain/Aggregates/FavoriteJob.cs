using Nexhire.Modules.SearchDiscovery.Core.Domain.Events;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;

public class FavoriteJob : AggregateRoot<Guid>
{
    public Guid SeekerUserId { get; private set; }
    public Guid PostingId { get; private set; }
    public DateTime FavoritedOnUtc { get; private set; }

    private FavoriteJob() { }

    public static Result<FavoriteJob> Add(Guid seekerUserId, Guid postingId, DateTime nowUtc)
    {
        if (seekerUserId == Guid.Empty)
            return Result.Failure<FavoriteJob>(new Error("FavoriteJob.EmptySeekerUserId", "Seeker user ID is required."));

        if (postingId == Guid.Empty)
            return Result.Failure<FavoriteJob>(new Error("FavoriteJob.EmptyPostingId", "Posting ID is required."));

        var favorite = new FavoriteJob
        {
            Id = Guid.NewGuid(),
            SeekerUserId = seekerUserId,
            PostingId = postingId,
            FavoritedOnUtc = nowUtc
        };

        favorite.RaiseDomainEvent(new JobFavorited(Guid.NewGuid(), seekerUserId, postingId, nowUtc));
        return Result.Success(favorite);
    }

    public Result Remove()
    {
        RaiseDomainEvent(new JobUnfavorited(Guid.NewGuid(), SeekerUserId, PostingId, DateTime.UtcNow));
        return Result.Success();
    }
}
