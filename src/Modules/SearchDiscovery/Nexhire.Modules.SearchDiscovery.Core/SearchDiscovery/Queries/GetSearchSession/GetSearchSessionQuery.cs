using Nexhire.Shared.Core.CQRS;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.GetSearchSession;

public record GetSearchSessionQuery(Guid SeekerUserId) : IQuery<SearchSessionDto>;
