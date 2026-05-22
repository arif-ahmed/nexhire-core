using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.GetFavorites;

public class GetFavoritesQueryHandler : IQueryHandler<GetFavoritesQuery, IReadOnlyList<FavoriteJobDto>>
{
    private readonly IFavoriteJobRepository _favoriteRepo;
    private readonly IJobIndexEntryRepository _jobIndexRepo;

    public GetFavoritesQueryHandler(IFavoriteJobRepository favoriteRepo, IJobIndexEntryRepository jobIndexRepo)
    {
        _favoriteRepo = favoriteRepo;
        _jobIndexRepo = jobIndexRepo;
    }

    public async Task<Result<IReadOnlyList<FavoriteJobDto>>> Handle(GetFavoritesQuery request, CancellationToken cancellationToken)
    {
        var favorites = await _favoriteRepo.ListBySeekerAsync(request.SeekerUserId, cancellationToken);

        var dtos = new List<FavoriteJobDto>();
        foreach (var fav in favorites)
        {
            var entry = await _jobIndexRepo.GetByIdAsync(fav.PostingId, cancellationToken);
            dtos.Add(new FavoriteJobDto(
                fav.Id, fav.PostingId,
                entry?.Title, entry?.CompanyName,
                fav.FavoritedOnUtc,
                entry is null));
        }

        return Result.Success<IReadOnlyList<FavoriteJobDto>>(dtos);
    }
}
