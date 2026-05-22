using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.DeleteSavedSearch;

public record DeleteSavedSearchCommand(
    Guid SavedSearchId,
    Guid SeekerUserId) : ICommand;
