using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Events;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Domain.Aggregates;

public class ReportArtifact : Entity<Guid>
{
    public Guid ReportRunId { get; private set; }
    public ExportFormat Format { get; private set; }
    public FileReference File { get; private set; } = null!;
    public DateTime GeneratedOnUtc { get; private set; }

    private ReportArtifact() { }
    internal ReportArtifact(Guid id, Guid runId, ExportFormat format, FileReference file) : base(id)
    {
        ReportRunId = runId; Format = format; File = file; GeneratedOnUtc = DateTime.UtcNow;
    }
}

public class ReportRun : AggregateRoot<Guid>
{
    private readonly List<ReportArtifact> _artifacts = new();
    private readonly List<ExportFormat> _requestedFormats = new();

    public Guid ReportDefinitionId { get; private set; }
    public int DefinitionVersionNumber { get; private set; }
    public RunTrigger Trigger { get; private set; } = null!;
    public ResolvedParameters Parameters { get; private set; } = null!;
    public RoleScope RoleScope { get; private set; } = null!;
    public ReportRunStatus Status { get; private set; }
    public IReadOnlyCollection<ReportArtifact> Artifacts => _artifacts.AsReadOnly();
    public IReadOnlyList<ExportFormat> RequestedFormats => _requestedFormats.AsReadOnly();
    public int? RowCount { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime QueuedOnUtc { get; private set; }
    public DateTime? StartedOnUtc { get; private set; }
    public DateTime? CompletedOnUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private ReportRun() { }

    public static Result<ReportRun> Queue(Guid definitionId, int definitionVersionNumber,
        RunTrigger trigger, ResolvedParameters parameters, RoleScope roleScope,
        List<ExportFormat> requestedFormats)
    {
        if (requestedFormats.Count == 0)
            return Result.Failure<ReportRun>(new Error("ReportRun.NoFormats", "At least one export format required."));

        var run = new ReportRun
        {
            Id = Guid.NewGuid(), ReportDefinitionId = definitionId,
            DefinitionVersionNumber = definitionVersionNumber, Trigger = trigger,
            Parameters = parameters, RoleScope = roleScope, Status = ReportRunStatus.Queued,
            QueuedOnUtc = DateTime.UtcNow
        };
        run._requestedFormats.AddRange(requestedFormats);
        run.RaiseDomainEvent(new ReportRunQueued(Guid.NewGuid(), run.Id, run.QueuedOnUtc));
        return Result.Success(run);
    }

    public Result MarkRunning()
    {
        if (Status != ReportRunStatus.Queued)
            return Result.Failure(new Error("ReportRun.InvalidTransition", $"Cannot transition from {Status} to Running."));
        Status = ReportRunStatus.Running;
        StartedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkCompleted(List<(ExportFormat Format, FileReference File)> artifacts, int rowCount)
    {
        if (Status != ReportRunStatus.Running)
            return Result.Failure(new Error("ReportRun.InvalidTransition", $"Cannot transition from {Status} to Completed."));

        foreach (var (format, file) in artifacts)
        {
            if (format != ExportFormat.Pdf && rowCount > 100_000)
                return Result.Failure(new Error("E-REPORT-ROW-LIMIT", "CSV/XLSX runs are capped at 100,000 rows."));
            _artifacts.Add(new ReportArtifact(Guid.NewGuid(), Id, format, file));
        }

        Status = ReportRunStatus.Completed;
        RowCount = rowCount;
        CompletedOnUtc = DateTime.UtcNow;

        var byUserId = Trigger.Mode == TriggerMode.OnDemand ? Trigger.UserId : null;
        RaiseDomainEvent(new ReportRunCompleted(Guid.NewGuid(), Id, ReportDefinitionId, byUserId, Trigger.ReportScheduleId, CompletedOnUtc.Value));
        return Result.Success();
    }

    public Result MarkFailed(string reason)
    {
        if (Status == ReportRunStatus.Completed)
            return Result.Failure(new Error("ReportRun.InvalidTransition", "Cannot fail a completed run."));
        Status = ReportRunStatus.Failed;
        FailureReason = reason;
        CompletedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ReportRunFailed(Guid.NewGuid(), Id, reason, CompletedOnUtc.Value));
        return Result.Success();
    }
}
