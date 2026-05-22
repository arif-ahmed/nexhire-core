using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.Events;

// Internal domain events
public record JobIndexed(
    Guid EventId,
    Guid PostingId,
    DateTime OccurredOnUtc) : IDomainEvent;

public record JobIndexUpdated(
    Guid EventId,
    Guid PostingId,
    DateTime OccurredOnUtc) : IDomainEvent;

public record JobFavorited(
    Guid EventId,
    Guid SeekerUserId,
    Guid PostingId,
    DateTime OccurredOnUtc) : IDomainEvent;

public record JobUnfavorited(
    Guid EventId,
    Guid SeekerUserId,
    Guid PostingId,
    DateTime OccurredOnUtc) : IDomainEvent;

public record SavedSearchCreated(
    Guid EventId,
    Guid SavedSearchId,
    Guid SeekerUserId,
    string Name,
    DateTime OccurredOnUtc) : IDomainEvent;

public record SavedSearchNotificationChanged(
    Guid EventId,
    Guid SavedSearchId,
    string Preference,
    DateTime OccurredOnUtc) : IDomainEvent;

// Integration events published to other modules
public record SearchPerformedIntegrationEvent(
    Guid EventId,
    Guid? UserId,
    string? Keyword,
    string FilterSummary,
    int ResultCount,
    DateTime OccurredOnUtc) : IDomainEvent;

public record SavedSearchCreatedIntegrationEvent(
    Guid EventId,
    Guid SavedSearchId,
    Guid SeekerUserId,
    string Name,
    string CriteriaSummary,
    string NotificationPreference,
    DateTime OccurredOnUtc) : IDomainEvent;

public record SavedSearchMatchFoundIntegrationEvent(
    Guid EventId,
    Guid SavedSearchId,
    Guid SeekerUserId,
    Guid[] PostingIds,
    string NotificationPreference,
    DateTime OccurredOnUtc) : IDomainEvent;
