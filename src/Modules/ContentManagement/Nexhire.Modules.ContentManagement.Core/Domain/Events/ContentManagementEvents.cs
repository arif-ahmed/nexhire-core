using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.ContentManagement.Core.Domain.Events;

public abstract record ContentManagementEvent(DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

// Article events
public sealed record ArticlePublishedDomainEvent(
    Guid ArticleId,
    DateTime OccurredOnUtc) : ContentManagementEvent(OccurredOnUtc)
{
    public ArticlePublishedDomainEvent(Guid articleId) : this(articleId, DateTime.UtcNow) { }
}

public sealed record ArticleScheduledDomainEvent(
    Guid ArticleId,
    DateTime PublishAtUtc,
    DateTime OccurredOnUtc) : ContentManagementEvent(OccurredOnUtc)
{
    public ArticleScheduledDomainEvent(Guid articleId, DateTime publishAtUtc) : this(articleId, publishAtUtc, DateTime.UtcNow) { }
}

public sealed record ArticleArchivedDomainEvent(
    Guid ArticleId,
    ArticleStatus ResultingStatus,
    DateTime OccurredOnUtc) : ContentManagementEvent(OccurredOnUtc)
{
    public ArticleArchivedDomainEvent(Guid articleId, ArticleStatus resultingStatus) : this(articleId, resultingStatus, DateTime.UtcNow) { }
}

// FAQ events
public sealed record FaqPublishedDomainEvent(
    Guid FaqEntryId,
    FaqEntryKind Kind,
    DateTime OccurredOnUtc) : ContentManagementEvent(OccurredOnUtc)
{
    public FaqPublishedDomainEvent(Guid faqEntryId, FaqEntryKind kind) : this(faqEntryId, kind, DateTime.UtcNow) { }
}

// Feedback events
public sealed record HelpFeedbackReceivedDomainEvent(
    Guid HelpFeedbackId,
    Guid FaqEntryId,
    bool WasHelpful,
    FeedbackReason? Reason,
    DateTime OccurredOnUtc) : ContentManagementEvent(OccurredOnUtc)
{
    public HelpFeedbackReceivedDomainEvent(Guid helpFeedbackId, Guid faqEntryId, bool wasHelpful, FeedbackReason? reason) : this(helpFeedbackId, faqEntryId, wasHelpful, reason, DateTime.UtcNow) { }
}
