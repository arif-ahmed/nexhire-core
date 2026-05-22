using Nexhire.Shared.Core.CQRS;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.SearchJobs;

public record SearchJobsQuery(
    string? Keyword,
    string? FiltersJson,
    Guid? SeekerUserId) : IQuery<SearchResultDto>;
