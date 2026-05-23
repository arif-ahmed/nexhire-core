using System;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.Notification.Core.Domain.Events;

public record NotificationCreated(Guid EventId, NotificationId NotificationId, Guid RecipientUserId, Channel Channel, NotificationType Type, Priority Priority, DateTime OccurredOnUtc) : IDomainEvent;

public record NotificationRead(Guid EventId, NotificationId NotificationId, Guid RecipientUserId, DateTime OccurredOnUtc) : IDomainEvent;

public record NotificationArchived(Guid EventId, NotificationId NotificationId, Guid RecipientUserId, DateTime OccurredOnUtc) : IDomainEvent;

public record TemplateVersionPublished(Guid EventId, NotificationTemplateId TemplateId, Channel Channel, NotificationType Type, int VersionNumber, DateTime OccurredOnUtc) : IDomainEvent;

public record NotificationPreferencesUpdated(Guid EventId, Guid UserId, DateTime OccurredOnUtc) : IDomainEvent;

public record DigestScheduled(Guid EventId, DigestId DigestId, Guid UserId, Channel Channel, DigestWindow Window, DateTime ScheduledSendUtc, DateTime OccurredOnUtc) : IDomainEvent;
