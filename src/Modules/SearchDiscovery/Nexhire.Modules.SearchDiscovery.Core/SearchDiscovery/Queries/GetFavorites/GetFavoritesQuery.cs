using Nexhire.Shared.Core.CQRS;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.GetFavorites;

public record GetFavoritesQuery(Guid SeekerUserId) : IQuery<IReadOnlyList<FavoriteJobDto>>;
