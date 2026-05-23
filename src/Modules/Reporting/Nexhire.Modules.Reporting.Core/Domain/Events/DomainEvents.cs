using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.Reporting.Core.Domain.Events;

public record ReportDefinitionCreated(Guid EventId, Guid ReportDefinitionId, DateTime OccurredOnUtc) : IDomainEvent;
public record ReportDefinitionVersioned(Guid EventId, Guid ReportDefinitionId, int NewVersionNumber, DateTime OccurredOnUtc) : IDomainEvent;
public record ReportRunQueued(Guid EventId, Guid ReportRunId, DateTime OccurredOnUtc) : IDomainEvent;
public record ReportRunCompleted(Guid EventId, Guid ReportRunId, Guid ReportDefinitionId, Guid? ByUserId, Guid? ScheduleId, DateTime OccurredOnUtc) : IDomainEvent;
public record ReportRunFailed(Guid EventId, Guid ReportRunId, string Reason, DateTime OccurredOnUtc) : IDomainEvent;
public record ReportScheduleCreated(Guid EventId, Guid ReportScheduleId, DateTime OccurredOnUtc) : IDomainEvent;
public record RetentionPolicyRevised(Guid EventId, Guid RetentionPolicyId, int NewVersionNumber, DateTime OccurredOnUtc) : IDomainEvent;
public record RetentionApplied(Guid EventId, Guid RetentionPolicyId, Guid RetentionRunId, int RecordsAffected, string ActionTaken, DateTime OccurredOnUtc) : IDomainEvent;
public record AlertIncidentRaised(Guid EventId, Guid AlertRuleId, Guid IncidentId, string MetricKey, string Severity, decimal ObservedValue, DateTime OccurredOnUtc) : IDomainEvent;
public record AlertIncidentEscalated(Guid EventId, Guid AlertRuleId, Guid IncidentId, DateTime OccurredOnUtc) : IDomainEvent;
