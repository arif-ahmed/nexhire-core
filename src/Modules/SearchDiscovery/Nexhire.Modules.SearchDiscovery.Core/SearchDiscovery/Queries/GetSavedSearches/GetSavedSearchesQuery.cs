using Nexhire.Shared.Core.CQRS;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.GetSavedSearches;

public record GetSavedSearchesQuery(Guid SeekerUserId) : IQuery<IReadOnlyList<SavedSearchDto>>;
