using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.SetSavedSearchNotification;

public record SetSavedSearchNotificationCommand(
    Guid SavedSearchId,
    Guid SeekerUserId,
    NotificationPreference Preference) : ICommand;
