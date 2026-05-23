using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Events;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Domain.Aggregates;

public class AlertIncident : Entity<Guid>
{
    public Guid AlertRuleId { get; private set; }
    public DateTime TriggeredOnUtc { get; private set; }
    public decimal ObservedValue { get; private set; }
    public IncidentTrigger Trigger { get; private set; }
    public IncidentState State { get; private set; }
    public Guid? AcknowledgedBy { get; private set; }
    public DateTime? SuppressedUntilUtc { get; private set; }
    public DateTime? StateChangedOnUtc { get; private set; }

    private AlertIncident() { }
    internal AlertIncident(Guid id, Guid ruleId, decimal observedValue, IncidentTrigger trigger) : base(id)
    {
        AlertRuleId = ruleId; ObservedValue = observedValue; Trigger = trigger;
        State = IncidentState.Raised; TriggeredOnUtc = DateTime.UtcNow;
    }

    internal bool IsUnresolved() => State == IncidentState.Raised;

    internal Result Acknowledge(Guid byUserId)
    {
        if (State != IncidentState.Raised)
            return Result.Failure(new Error("AlertIncident.NotRaised", "Only raised incidents can be acknowledged."));
        State = IncidentState.Acknowledged; AcknowledgedBy = byUserId; StateChangedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    internal void Suppress(DateTime untilUtc)
    {
        State = IncidentState.Suppressed; SuppressedUntilUtc = untilUtc; StateChangedOnUtc = DateTime.UtcNow;
    }

    internal void Escalate() { State = IncidentState.Escalated; StateChangedOnUtc = DateTime.UtcNow; }
}

public class AlertRule : AggregateRoot<Guid>
{
    private readonly List<AlertIncident> _incidents = new();
    private readonly List<AlertChannel> _channels = new();

    public string Name { get; private set; } = null!;
    public string MetricKey { get; private set; } = null!;
    public AlertCondition Condition { get; private set; } = null!;
    public AlertSeverity Severity { get; private set; }
    public IReadOnlyCollection<AlertChannel> Channels => _channels.AsReadOnly();
    public bool AnomalyDetectionEnabled { get; private set; }
    public AlertRuleStatus Status { get; private set; }
    public IReadOnlyCollection<AlertIncident> Incidents => _incidents.AsReadOnly();
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private AlertRule() { }

    public static Result<AlertRule> Create(string name, string metricKey, AlertCondition condition,
        AlertSeverity severity, List<AlertChannel> channels, bool anomalyDetectionEnabled)
    {
        if (string.IsNullOrWhiteSpace(metricKey)) return Result.Failure<AlertRule>(new Error("AlertRule.EmptyMetricKey", "MetricKey required."));
        if (channels.Count == 0) return Result.Failure<AlertRule>(new Error("AlertRule.NoChannels", "At least one channel required."));

        var now = DateTime.UtcNow;
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(), Name = name, MetricKey = metricKey, Condition = condition,
            Severity = severity, AnomalyDetectionEnabled = anomalyDetectionEnabled,
            Status = AlertRuleStatus.Enabled, CreatedOnUtc = now, UpdatedOnUtc = now
        };
        rule._channels.AddRange(channels);
        return Result.Success(rule);
    }

    public Result UpdateCondition(AlertCondition newCondition) { Condition = newCondition; UpdatedOnUtc = DateTime.UtcNow; return Result.Success(); }
    public Result UpdateSeverity(AlertSeverity newSeverity) { Severity = newSeverity; UpdatedOnUtc = DateTime.UtcNow; return Result.Success(); }
    public Result UpdateChannels(List<AlertChannel> channels)
    {
        if (channels.Count == 0) return Result.Failure(new Error("AlertRule.NoChannels", "At least one channel required."));
        _channels.Clear(); _channels.AddRange(channels); UpdatedOnUtc = DateTime.UtcNow; return Result.Success();
    }

    public void Enable() { Status = AlertRuleStatus.Enabled; UpdatedOnUtc = DateTime.UtcNow; }
    public void Disable() { Status = AlertRuleStatus.Disabled; UpdatedOnUtc = DateTime.UtcNow; }

    public Result Fire(decimal observedValue, IncidentTrigger trigger, DateTime firedOnUtc)
    {
        if (Status == AlertRuleStatus.Disabled)
            return Result.Failure(new Error("AlertRule.Disabled", "Disabled rules cannot fire."));
        if (_incidents.Any(i => i.IsUnresolved()))
            return Result.Success(); // de-dup: silent no-op

        var incident = new AlertIncident(Guid.NewGuid(), Id, observedValue, trigger);
        _incidents.Add(incident);
        RaiseDomainEvent(new AlertIncidentRaised(Guid.NewGuid(), Id, incident.Id, MetricKey, Severity.ToString(), observedValue, firedOnUtc));
        return Result.Success();
    }

    public Result AcknowledgeIncident(Guid incidentId, Guid byUserId)
    {
        var incident = _incidents.FirstOrDefault(i => i.Id == incidentId);
        if (incident is null) return Result.Failure(new Error("AlertIncident.NotFound", "Incident not found."));
        return incident.Acknowledge(byUserId);
    }

    public Result SuppressIncident(Guid incidentId, DateTime untilUtc)
    {
        var incident = _incidents.FirstOrDefault(i => i.Id == incidentId);
        if (incident is null) return Result.Failure(new Error("AlertIncident.NotFound", "Incident not found."));
        incident.Suppress(untilUtc);
        return Result.Success();
    }

    public Result EscalateIncident(Guid incidentId)
    {
        var incident = _incidents.FirstOrDefault(i => i.Id == incidentId);
        if (incident is null) return Result.Failure(new Error("AlertIncident.NotFound", "Incident not found."));
        incident.Escalate();
        RaiseDomainEvent(new AlertIncidentEscalated(Guid.NewGuid(), Id, incidentId, DateTime.UtcNow));
        return Result.Success();
    }
}
