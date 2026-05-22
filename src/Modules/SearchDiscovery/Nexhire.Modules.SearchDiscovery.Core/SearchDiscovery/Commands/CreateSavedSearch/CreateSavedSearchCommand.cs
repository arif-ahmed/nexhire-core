using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.CreateSavedSearch;

public record CreateSavedSearchCommand(
    Guid SeekerUserId,
    string Name,
    string? Keyword,
    string? FiltersJson,
    NotificationPreference NotificationPreference) : ICommand<Guid>;
