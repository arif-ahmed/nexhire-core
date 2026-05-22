using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RenameSavedSearch;

public record RenameSavedSearchCommand(
    Guid SavedSearchId,
    Guid SeekerUserId,
    string NewName) : ICommand;
