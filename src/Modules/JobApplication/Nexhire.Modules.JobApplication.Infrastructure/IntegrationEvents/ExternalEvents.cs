using System;
using MediatR;

namespace Nexhire.Modules.JobApplication.Infrastructure.IntegrationEvents;

public record AccountDeactivatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid UserId) : INotification;

public record JobPostingClosedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid PostingId) : INotification;
