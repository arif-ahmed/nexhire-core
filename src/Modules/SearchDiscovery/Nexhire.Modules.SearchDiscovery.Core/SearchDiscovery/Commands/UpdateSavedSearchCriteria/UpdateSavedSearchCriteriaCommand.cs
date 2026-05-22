using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.UpdateSavedSearchCriteria;

public record UpdateSavedSearchCriteriaCommand(
    Guid SavedSearchId,
    Guid SeekerUserId,
    string? Keyword,
    string? FiltersJson) : ICommand;
