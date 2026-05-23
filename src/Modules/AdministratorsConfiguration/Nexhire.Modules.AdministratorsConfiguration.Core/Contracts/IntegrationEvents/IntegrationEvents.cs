using MediatR;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Contracts.IntegrationEvents;

public sealed record TaxonomyUpdatedIntegrationEvent(
    Guid EventId,
    Guid TaxonomyId,
    string Kind,
    int Version,
    string ChangeSummary,
    DateTime OccurredOnUtc) : INotification;

public sealed record TaxonomyTermAddedIntegrationEvent(
    Guid EventId,
    Guid TaxonomyId,
    string Kind,
    string TermCode,
    string Label,
    string? Category,
    string? ParentCode,
    DateTime OccurredOnUtc) : INotification;

public sealed record TaxonomyTermDeprecatedIntegrationEvent(
    Guid EventId,
    Guid TaxonomyId,
    string Kind,
    string TermCode,
    string? ReplacedByCode,
    DateTime OccurredOnUtc) : INotification;
