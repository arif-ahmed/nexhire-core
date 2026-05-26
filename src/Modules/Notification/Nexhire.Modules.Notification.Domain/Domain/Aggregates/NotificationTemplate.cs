using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;
using Nexhire.Modules.Notification.Domain.Events;

namespace Nexhire.Modules.Notification.Domain.Aggregates;

public sealed class NotificationTemplate : AggregateRoot<NotificationTemplateId>
{
    private readonly List<TemplateVersion> _history = new();

    public Channel Channel { get; private set; }
    public NotificationType Type { get; private set; }
    public string Name { get; private set; } = null!;
    public TemplateVersion CurrentVersion { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    public IReadOnlyCollection<TemplateVersion> History => _history.AsReadOnly();

    private NotificationTemplate() { } // EF Core

    private NotificationTemplate(
        NotificationTemplateId id,
        Channel channel,
        NotificationType type,
        string name,
        TemplateVersion initialVersion,
        DateTime nowUtc) : base(id)
    {
        Channel = channel;
        Type = type;
        Name = name;
        CurrentVersion = initialVersion;
        IsActive = true;
        CreatedOnUtc = nowUtc;
        UpdatedOnUtc = nowUtc;
    }

    public static Result<NotificationTemplate> Create(
        Channel channel,
        NotificationType type,
        string name,
        TemplateVersion initialVersion,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<NotificationTemplate>(new Error("Template.NameRequired", "Template name is required."));
        if (initialVersion is null)
            return Result.Failure<NotificationTemplate>(new Error("Template.VersionRequired", "Initial version is required."));

        var id = new NotificationTemplateId(Guid.NewGuid());
        var template = new NotificationTemplate(id, channel, type, name, initialVersion, nowUtc);

        return template;
    }

    public Result PublishNewVersion(TemplateVersion newVersion, Guid createdByUserId)
    {
        if (newVersion is null)
            return Result.Failure(new Error("Template.NewVersionRequired", "New template version is required."));

        if (newVersion.VersionNumber != CurrentVersion.VersionNumber + 1)
            return Result.Failure(new Error("Template.InvalidSequence", $"Version number must be sequential. Expected {CurrentVersion.VersionNumber + 1}, got {newVersion.VersionNumber}."));

        // Add old current to history
        _history.Add(CurrentVersion);

        CurrentVersion = newVersion;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new TemplateVersionPublished(
            Guid.NewGuid(),
            Id,
            Channel,
            Type,
            CurrentVersion.VersionNumber,
            DateTime.UtcNow));

        return Result.Success();
    }

    public Result RollbackTo(int versionNumber, Guid createdByUserId)
    {
        TemplateVersion? target = null;
        if (CurrentVersion.VersionNumber == versionNumber)
        {
            return Result.Success(); // already on it
        }

        target = _history.FirstOrDefault(v => v.VersionNumber == versionNumber);
        if (target == null)
            return Result.Failure(new Error("Template.VersionNotFound", $"Version {versionNumber} was not found in template history."));

        // Copy history version as a new current version to preserve append-only history audit
        int nextVersionNo = CurrentVersion.VersionNumber + 1;
        var rolledVersion = TemplateVersion.Create(
            nextVersionNo,
            target.Subject,
            target.BodyHtml,
            target.BodyText,
            target.Footer,
            target.Placeholders,
            DateTime.UtcNow,
            createdByUserId).Value;

        return PublishNewVersion(rolledVersion, createdByUserId);
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void PurgeHistoryOlderThan(DateTime cutoffUtc)
    {
        _history.RemoveAll(v => v.CreatedOnUtc < cutoffUtc);
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
