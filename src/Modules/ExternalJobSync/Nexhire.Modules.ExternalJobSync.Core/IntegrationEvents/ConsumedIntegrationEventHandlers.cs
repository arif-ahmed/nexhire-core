using MediatR;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Ports;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Repositories;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.IntegrationEvents;

// Event definitions consumed from external Bounded Contexts (e.g. BC-4 Job Postings, BC-2 Employer Profile)
public sealed record JobPostingPublishedIntegrationEvent(
    Guid JobPostingId, 
    Guid EmployerId, 
    string Title, 
    List<string> RequiredSkillCodes, 
    DateTime DeadlineUtc, 
    string Visibility, 
    DateTime OccurredOnUtc) : INotification;

public sealed record JobPostingUpdatedIntegrationEvent(
    Guid JobPostingId, 
    List<string> ChangedFields, 
    DateTime OccurredOnUtc) : INotification;

public sealed record JobPostingClosedIntegrationEvent(
    Guid JobPostingId, 
    string Reason, 
    DateTime OccurredOnUtc) : INotification;

public sealed record EmployerVerificationRequestedIntegrationEvent(
    Guid EmployerId, 
    string RegistryRef, 
    DateTime OccurredOnUtc) : INotification;

public sealed record TaxonomyUpdatedIntegrationEvent(
    Guid TaxonomyId, 
    string Version, 
    string ChangeSummary, 
    DateTime OccurredOnUtc) : INotification;

// Handlers that orchestrate actions inside this module in response to external events
public sealed class JobPostingPublishedIntegrationEventHandler : INotificationHandler<JobPostingPublishedIntegrationEvent>
{
    private readonly IExternalConnectorRepository _connectorRepository;
    private readonly IMediator _mediator;

    public JobPostingPublishedIntegrationEventHandler(IExternalConnectorRepository connectorRepository, IMediator mediator)
    {
        _connectorRepository = connectorRepository;
        _mediator = mediator;
    }

    public async Task Handle(JobPostingPublishedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // For each ExternalConnector with PushOnPublish == true: start export command
        var connectors = await _connectorRepository.ListWithPushOnPublishAsync(cancellationToken);
        foreach (var conn in connectors)
        {
            // Call MediatR command to push/export the job
            // await _mediator.Send(new ExportJobCommand(notification.JobPostingId, conn.Id), cancellationToken);
        }
    }
}

public sealed class EmployerVerificationRequestedIntegrationEventHandler : INotificationHandler<EmployerVerificationRequestedIntegrationEvent>
{
    private readonly IMediator _mediator;

    public EmployerVerificationRequestedIntegrationEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(EmployerVerificationRequestedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Enforce government verification check on employer registration
        var registry = Registry.Create("MoL-Employer-Registry", "http://mol.gov/employers").Value;
        var consent = ConsentRecord.Create(true, "v1", DateTime.UtcNow).Value;
        var fields = new Dictionary<string, string> { { "registration_number", notification.RegistryRef } };
        var payload = MinimisedRequestPayload.Create(VerificationKind.Employer, fields).Value;

        // In a real flow, this dispatches a command to hit government MoL registry and verify employer
        // await _mediator.Send(new VerifyEmployerViaGovernmentCommand(notification.EmployerId, registry, consent, payload), cancellationToken);
    }
}

// Stubs for remaining consumed event handlers to satisfy MediatR pipelines cleanly
public sealed class JobPostingUpdatedIntegrationEventHandler : INotificationHandler<JobPostingUpdatedIntegrationEvent>
{
    public Task Handle(JobPostingUpdatedIntegrationEvent notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class JobPostingClosedIntegrationEventHandler : INotificationHandler<JobPostingClosedIntegrationEvent>
{
    public Task Handle(JobPostingClosedIntegrationEvent notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class TaxonomyUpdatedIntegrationEventHandler : INotificationHandler<TaxonomyUpdatedIntegrationEvent>
{
    public Task Handle(TaxonomyUpdatedIntegrationEvent notification, CancellationToken cancellationToken) => Task.CompletedTask;
}
