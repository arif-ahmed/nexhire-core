using MediatR;
using Nexhire.Modules.AdministratorsConfiguration.Core.Contracts.IntegrationEvents;
using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Events;

namespace Nexhire.Modules.AdministratorsConfiguration.Infrastructure.IntegrationEvents.Publishers;

public sealed class IntegrationEventPublisher :
    INotificationHandler<TaxonomyTermAddedDomainEvent>,
    INotificationHandler<TaxonomyTermDeprecatedDomainEvent>,
    INotificationHandler<TaxonomyUpdatedDomainEvent>
{
    private readonly IPublisher _publisher;

    public IntegrationEventPublisher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(TaxonomyTermAddedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new TaxonomyTermAddedIntegrationEvent(
            notification.EventId,
            notification.TaxonomyId,
            notification.Kind.ToString(),
            notification.Code.Value,
            notification.Label,
            notification.Category?.ToString(),
            notification.ParentCode?.Value,
            notification.OccurredOnUtc);

        await _publisher.Publish(integrationEvent, cancellationToken);
    }

    public async Task Handle(TaxonomyTermDeprecatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new TaxonomyTermDeprecatedIntegrationEvent(
            notification.EventId,
            notification.TaxonomyId,
            notification.Kind.ToString(),
            notification.Code.Value,
            notification.ReplacedByCode?.Value,
            notification.OccurredOnUtc);

        await _publisher.Publish(integrationEvent, cancellationToken);
    }

    public async Task Handle(TaxonomyUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new TaxonomyUpdatedIntegrationEvent(
            notification.EventId,
            notification.TaxonomyId,
            notification.Kind.ToString(),
            notification.Version,
            notification.ChangeSummary,
            notification.OccurredOnUtc);

        await _publisher.Publish(integrationEvent, cancellationToken);
    }
}
