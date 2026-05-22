using Nexhire.Shared.Core.CQRS;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.RunSavedSearch;

public record RunSavedSearchQuery(
    Guid SavedSearchId,
    Guid SeekerUserId) : IQuery<SearchResultDto>;
