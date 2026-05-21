using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Shared.Infrastructure.Interceptors;

public class PublishDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public PublishDomainEventsInterceptor(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        PublishDomainEvents(eventData.Context).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await PublishDomainEvents(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task PublishDomainEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries().ToList();
        var domainEvents = new List<IDomainEvent>();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();
            var baseType = entityType;

            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
                {
                    var domainEventsProperty = baseType.GetProperty(nameof(AggregateRoot<object>.DomainEvents));
                    var clearMethod = baseType.GetMethod(nameof(AggregateRoot<object>.ClearDomainEvents));

                    if (domainEventsProperty != null && clearMethod != null)
                    {
                        var events = domainEventsProperty.GetValue(entry.Entity) as IReadOnlyCollection<IDomainEvent>;
                        if (events != null && events.Any())
                        {
                            domainEvents.AddRange(events);
                            clearMethod.Invoke(entry.Entity, null);
                        }
                    }
                    break;
                }
                baseType = baseType.BaseType;
            }
        }

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent);
        }
    }
}
