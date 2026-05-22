using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RememberSearchCriteria;

public record RememberSearchCriteriaCommand(
    Guid SeekerUserId,
    string? Keyword,
    string? FiltersJson) : ICommand;
