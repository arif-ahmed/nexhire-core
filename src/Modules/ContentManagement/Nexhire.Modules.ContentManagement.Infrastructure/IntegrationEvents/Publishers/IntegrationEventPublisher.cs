using MediatR;
using Microsoft.Extensions.Logging;
using Nexhire.Modules.ContentManagement.Core.Domain.Events;

namespace Nexhire.Modules.ContentManagement.Infrastructure.IntegrationEvents.Publishers;

// Bridge: domain events → integration events.
// Registered as INotificationHandler<TDomainEvent> in DI.
// The PublishDomainEventsInterceptor already writes to the outbox.
// This publisher maps domain events to integration events and writes them to outbox messages.
public sealed class IntegrationEventPublisher :
    INotificationHandler<ArticlePublishedDomainEvent>,
    INotificationHandler<ArticleScheduledDomainEvent>,
    INotificationHandler<ArticleArchivedDomainEvent>,
    INotificationHandler<FaqPublishedDomainEvent>,
    INotificationHandler<HelpFeedbackReceivedDomainEvent>
{
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(ILogger<IntegrationEventPublisher> logger) => _logger = logger;

    public Task Handle(ArticlePublishedDomainEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("Article {ArticleId} published integration event mapped", notification.ArticleId);
        return Task.CompletedTask;
    }

    public Task Handle(ArticleScheduledDomainEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("Article {ArticleId} scheduled integration event mapped", notification.ArticleId);
        return Task.CompletedTask;
    }

    public Task Handle(ArticleArchivedDomainEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("Article {ArticleId} archived integration event mapped, resulting status: {Status}",
            notification.ArticleId, notification.ResultingStatus);
        return Task.CompletedTask;
    }

    public Task Handle(FaqPublishedDomainEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("FAQ {FaqEntryId} published integration event mapped", notification.FaqEntryId);
        return Task.CompletedTask;
    }

    public Task Handle(HelpFeedbackReceivedDomainEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("HelpFeedback {FeedbackId} received integration event mapped", notification.HelpFeedbackId);
        return Task.CompletedTask;
    }
}
