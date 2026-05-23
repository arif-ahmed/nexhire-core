using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Events;
using Nexhire.Modules.Reporting.Core.Domain.Services;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Domain.Aggregates;

public class ReportSchedule : AggregateRoot<Guid>
{
    private readonly List<EmailAddress> _distributionList = new();
    private readonly List<ExportFormat> _exportFormats = new();

    public Guid ReportDefinitionId { get; private set; }
    public ScheduleCadence Cadence { get; private set; } = null!;
    public ResolvedParameters Parameters { get; private set; } = null!;
    public IReadOnlyCollection<EmailAddress> DistributionList => _distributionList.AsReadOnly();
    public IReadOnlyCollection<ExportFormat> ExportFormats => _exportFormats.AsReadOnly();
    public ScheduleStatus Status { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public DateTime? LastRunOnUtc { get; private set; }
    public DateTime NextRunOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private ReportSchedule() { }

    public static Result<ReportSchedule> Create(Guid definitionId, ScheduleCadence cadence,
        ResolvedParameters parameters, List<EmailAddress> distributionList,
        List<ExportFormat> exportFormats, Guid ownerUserId)
    {
        if (distributionList.Count == 0)
            return Result.Failure<ReportSchedule>(new Error("ReportSchedule.EmptyDistribution", "Distribution list cannot be empty."));
        if (exportFormats.Count == 0)
            return Result.Failure<ReportSchedule>(new Error("ReportSchedule.NoFormats", "Export formats cannot be empty."));

        var now = DateTime.UtcNow;
        var schedule = new ReportSchedule
        {
            Id = Guid.NewGuid(), ReportDefinitionId = definitionId, Cadence = cadence,
            Parameters = parameters, Status = ScheduleStatus.Active,
            OwnerUserId = ownerUserId, CreatedOnUtc = now, UpdatedOnUtc = now,
            NextRunOnUtc = ScheduleNextRunCalculator.ComputeNextRun(cadence, now)
        };
        schedule._distributionList.AddRange(distributionList);
        schedule._exportFormats.AddRange(exportFormats);
        schedule.RaiseDomainEvent(new ReportScheduleCreated(Guid.NewGuid(), schedule.Id, now));
        return Result.Success(schedule);
    }

    public Result UpdateCadence(ScheduleCadence newCadence)
    {
        if (Status != ScheduleStatus.Active)
            return Result.Failure(new Error("ReportSchedule.NotActive", "Only active schedules can be updated."));
        Cadence = newCadence;
        NextRunOnUtc = ScheduleNextRunCalculator.ComputeNextRun(newCadence, DateTime.UtcNow);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateDistributionList(List<EmailAddress> list)
    {
        if (list.Count == 0)
            return Result.Failure(new Error("ReportSchedule.EmptyDistribution", "Distribution list cannot be empty."));
        _distributionList.Clear();
        _distributionList.AddRange(list);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Pause()
    {
        if (Status != ScheduleStatus.Active)
            return Result.Failure(new Error("ReportSchedule.NotActive", "Only active schedules can be paused."));
        Status = ScheduleStatus.Paused;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Resume()
    {
        if (Status != ScheduleStatus.Paused)
            return Result.Failure(new Error("ReportSchedule.NotPaused", "Only paused schedules can be resumed."));
        Status = ScheduleStatus.Active;
        NextRunOnUtc = ScheduleNextRunCalculator.ComputeNextRun(Cadence, DateTime.UtcNow);
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void RecordRun(DateTime executedOnUtc)
    {
        LastRunOnUtc = executedOnUtc;
        NextRunOnUtc = ScheduleNextRunCalculator.ComputeNextRun(Cadence, executedOnUtc);
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
