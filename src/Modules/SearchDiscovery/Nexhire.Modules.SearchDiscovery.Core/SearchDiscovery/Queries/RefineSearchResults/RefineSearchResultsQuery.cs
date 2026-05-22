using Nexhire.Shared.Core.CQRS;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.RefineSearchResults;

public record RefineSearchResultsQuery(
    Guid SeekerUserId,
    string? FiltersJson) : IQuery<SearchResultDto>;
