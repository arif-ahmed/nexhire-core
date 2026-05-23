using FluentAssertions;
using Nexhire.Modules.Reporting.Core.Domain.Aggregates;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;

namespace Nexhire.Modules.Reporting.Tests.Unit.Domain;

public class ReportDefinitionTests
{
    private static ReportSpec ValidSpec() =>
        ReportSpec.Create(new List<string> { "posting.volume" }, new List<string>(), new List<ReportFilter>(),
            VisualizationType.Table, _ => true, _ => true).Value;

    private static ReportVisibility AdminVisibility() =>
        ReportVisibility.Create(new HashSet<string> { "SystemAdministrator" }).Value;

    [Fact]
    public void CreateTemplate_ValidInputs_Succeeds()
    {
        var result = ReportDefinition.CreateTemplate("My Template", ReportCategory.EmploymentStats,
            Guid.NewGuid(), ValidSpec(), new List<ConfigurableParameter>(), AdminVisibility());
        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(ReportDefinitionKind.Template);
        result.Value.Status.Should().Be(ReportDefinitionStatus.Active);
        result.Value.CurrentVersionNumber.Should().Be(1);
        result.Value.Versions.Should().HaveCount(1);
    }

    [Fact]
    public void CreateTemplate_EmptyName_Fails()
    {
        var result = ReportDefinition.CreateTemplate("", ReportCategory.EmploymentStats,
            Guid.NewGuid(), ValidSpec(), new List<ConfigurableParameter>(), AdminVisibility());
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ReportDefinition.InvalidName");
    }

    [Fact]
    public void CreateCustom_ValidInputs_IsCustomKind()
    {
        var result = ReportDefinition.CreateCustom("My Custom", Guid.NewGuid(), ValidSpec());
        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(ReportDefinitionKind.Custom);
    }

    [Fact]
    public void SaveCustomAsTemplate_OnTemplate_Fails()
    {
        var def = ReportDefinition.CreateTemplate("T", ReportCategory.EmploymentStats,
            Guid.NewGuid(), ValidSpec(), new List<ConfigurableParameter>(), AdminVisibility()).Value;
        var result = def.SaveCustomAsTemplate(ReportCategory.Custom, new List<ConfigurableParameter>(), AdminVisibility());
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-REPORT-NOT-CUSTOM");
    }

    [Fact]
    public void SaveCustomAsTemplate_OnCustom_PromotesToTemplate()
    {
        var def = ReportDefinition.CreateCustom("C", Guid.NewGuid(), ValidSpec()).Value;
        var result = def.SaveCustomAsTemplate(ReportCategory.EmploymentStats, new List<ConfigurableParameter>(), AdminVisibility());
        result.IsSuccess.Should().BeTrue();
        def.Kind.Should().Be(ReportDefinitionKind.Template);
    }

    [Fact]
    public void UpdateSpec_AppendsVersionAndIncrements()
    {
        var def = ReportDefinition.CreateTemplate("T", ReportCategory.EmploymentStats,
            Guid.NewGuid(), ValidSpec(), new List<ConfigurableParameter>(), AdminVisibility()).Value;
        def.UpdateSpec(ValidSpec(), Guid.NewGuid());
        def.CurrentVersionNumber.Should().Be(2);
        def.Versions.Should().HaveCount(2);
        def.Versions.Count(v => v.IsCurrent).Should().Be(1);
    }

    [Fact]
    public void UpdateSpec_OldVersionsMarkedNotCurrent()
    {
        var def = ReportDefinition.CreateTemplate("T", ReportCategory.EmploymentStats,
            Guid.NewGuid(), ValidSpec(), new List<ConfigurableParameter>(), AdminVisibility()).Value;
        def.UpdateSpec(ValidSpec(), Guid.NewGuid());
        def.Versions.First(v => v.VersionNumber == 1).IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void RecordUsage_IncrementsUsageCount()
    {
        var def = ReportDefinition.CreateTemplate("T", ReportCategory.EmploymentStats,
            Guid.NewGuid(), ValidSpec(), new List<ConfigurableParameter>(), AdminVisibility()).Value;
        def.RecordUsage();
        def.RecordUsage();
        def.UsageCount.Should().Be(2);
    }

    [Fact]
    public void Archive_ActiveDef_Succeeds()
    {
        var def = ReportDefinition.CreateTemplate("T", ReportCategory.EmploymentStats,
            Guid.NewGuid(), ValidSpec(), new List<ConfigurableParameter>(), AdminVisibility()).Value;
        var result = def.Archive();
        result.IsSuccess.Should().BeTrue();
        def.Status.Should().Be(ReportDefinitionStatus.Archived);
    }

    [Fact]
    public void Archive_AlreadyArchived_Fails()
    {
        var def = ReportDefinition.CreateTemplate("T", ReportCategory.EmploymentStats,
            Guid.NewGuid(), ValidSpec(), new List<ConfigurableParameter>(), AdminVisibility()).Value;
        def.Archive();
        var result = def.Archive();
        result.IsFailure.Should().BeTrue();
    }
}

public class ReportRunTests
{
    private static RunTrigger OnDemandTrigger() =>
        RunTrigger.CreateOnDemand(Guid.NewGuid()).Value;

    private static ResolvedParameters EmptyParams() =>
        ResolvedParameters.Empty();

    private static RoleScope AdminScope() =>
        RoleScope.Create(RoleName.SystemAdministrator, null).Value;

    private static FileReference ValidFile() =>
        FileReference.Create("storage/key", "file.pdf", "application/pdf", 1024).Value;

    [Fact]
    public void Queue_NoFormats_Fails()
    {
        var result = ReportRun.Queue(Guid.NewGuid(), 1, OnDemandTrigger(), EmptyParams(), AdminScope(), new List<ExportFormat>());
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ReportRun.NoFormats");
    }

    [Fact]
    public void Queue_ValidInputs_StatusQueued()
    {
        var result = ReportRun.Queue(Guid.NewGuid(), 1, OnDemandTrigger(), EmptyParams(), AdminScope(),
            new List<ExportFormat> { ExportFormat.Pdf });
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReportRunStatus.Queued);
    }

    [Fact]
    public void MarkRunning_FromQueued_Succeeds()
    {
        var run = ReportRun.Queue(Guid.NewGuid(), 1, OnDemandTrigger(), EmptyParams(), AdminScope(),
            new List<ExportFormat> { ExportFormat.Pdf }).Value;
        run.MarkRunning().IsSuccess.Should().BeTrue();
        run.Status.Should().Be(ReportRunStatus.Running);
    }

    [Fact]
    public void MarkRunning_FromRunning_Fails()
    {
        var run = ReportRun.Queue(Guid.NewGuid(), 1, OnDemandTrigger(), EmptyParams(), AdminScope(),
            new List<ExportFormat> { ExportFormat.Pdf }).Value;
        run.MarkRunning();
        run.MarkRunning().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkCompleted_FromRunning_AddsArtifacts()
    {
        var run = ReportRun.Queue(Guid.NewGuid(), 1, OnDemandTrigger(), EmptyParams(), AdminScope(),
            new List<ExportFormat> { ExportFormat.Pdf }).Value;
        run.MarkRunning();
        var result = run.MarkCompleted(new List<(ExportFormat, FileReference)> { (ExportFormat.Pdf, ValidFile()) }, 100);
        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(ReportRunStatus.Completed);
        run.Artifacts.Should().HaveCount(1);
        run.RowCount.Should().Be(100);
    }

    [Fact]
    public void MarkCompleted_CsvOver100k_Fails()
    {
        var run = ReportRun.Queue(Guid.NewGuid(), 1, OnDemandTrigger(), EmptyParams(), AdminScope(),
            new List<ExportFormat> { ExportFormat.Csv }).Value;
        run.MarkRunning();
        var result = run.MarkCompleted(new List<(ExportFormat, FileReference)> { (ExportFormat.Csv, ValidFile()) }, 100_001);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-REPORT-ROW-LIMIT");
    }

    [Fact]
    public void MarkFailed_FromQueued_Succeeds()
    {
        var run = ReportRun.Queue(Guid.NewGuid(), 1, OnDemandTrigger(), EmptyParams(), AdminScope(),
            new List<ExportFormat> { ExportFormat.Pdf }).Value;
        run.MarkFailed("timeout").IsSuccess.Should().BeTrue();
        run.Status.Should().Be(ReportRunStatus.Failed);
        run.FailureReason.Should().Be("timeout");
    }

    [Fact]
    public void MarkFailed_FromCompleted_Fails()
    {
        var run = ReportRun.Queue(Guid.NewGuid(), 1, OnDemandTrigger(), EmptyParams(), AdminScope(),
            new List<ExportFormat> { ExportFormat.Pdf }).Value;
        run.MarkRunning();
        run.MarkCompleted(new List<(ExportFormat, FileReference)> { (ExportFormat.Pdf, ValidFile()) }, 1);
        run.MarkFailed("oops").IsFailure.Should().BeTrue();
    }
}

public class ReportScheduleTests
{
    private static ScheduleCadence DailyCadence() =>
        ScheduleCadence.Create(Frequency.Daily, null, null, new TimeOnly(8, 0), new List<DateOnly>()).Value;

    private static ScheduleCadence WeeklyCadence() =>
        ScheduleCadence.Create(Frequency.Weekly, DayOfWeek.Monday, null, new TimeOnly(8, 0), new List<DateOnly>()).Value;

    private static ResolvedParameters EmptyParams() => ResolvedParameters.Empty();

    private static EmailAddress TestEmail() => EmailAddress.Create("test@example.com").Value;

    [Fact]
    public void Create_ValidInputs_NextRunInFuture()
    {
        var result = ReportSchedule.Create(Guid.NewGuid(), DailyCadence(), EmptyParams(),
            new List<EmailAddress> { TestEmail() }, new List<ExportFormat> { ExportFormat.Pdf }, Guid.NewGuid());
        result.IsSuccess.Should().BeTrue();
        result.Value.NextRunOnUtc.Should().BeAfter(DateTime.UtcNow);
        result.Value.Status.Should().Be(ScheduleStatus.Active);
    }

    [Fact]
    public void Create_EmptyDistribution_Fails()
    {
        var result = ReportSchedule.Create(Guid.NewGuid(), DailyCadence(), EmptyParams(),
            new List<EmailAddress>(), new List<ExportFormat> { ExportFormat.Pdf }, Guid.NewGuid());
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Pause_ActiveSchedule_Pauses()
    {
        var schedule = ReportSchedule.Create(Guid.NewGuid(), DailyCadence(), EmptyParams(),
            new List<EmailAddress> { TestEmail() }, new List<ExportFormat> { ExportFormat.Pdf }, Guid.NewGuid()).Value;
        schedule.Pause().IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Paused);
    }

    [Fact]
    public void Pause_AlreadyPaused_Fails()
    {
        var schedule = ReportSchedule.Create(Guid.NewGuid(), DailyCadence(), EmptyParams(),
            new List<EmailAddress> { TestEmail() }, new List<ExportFormat> { ExportFormat.Pdf }, Guid.NewGuid()).Value;
        schedule.Pause();
        schedule.Pause().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Resume_PausedSchedule_ActiveAndNextRunUpdated()
    {
        var schedule = ReportSchedule.Create(Guid.NewGuid(), DailyCadence(), EmptyParams(),
            new List<EmailAddress> { TestEmail() }, new List<ExportFormat> { ExportFormat.Pdf }, Guid.NewGuid()).Value;
        schedule.Pause();
        schedule.Resume().IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Active);
        schedule.NextRunOnUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void RecordRun_AdvancesNextRunOnUtc()
    {
        var schedule = ReportSchedule.Create(Guid.NewGuid(), WeeklyCadence(), EmptyParams(),
            new List<EmailAddress> { TestEmail() }, new List<ExportFormat> { ExportFormat.Pdf }, Guid.NewGuid()).Value;
        // Simulate last run was the original NextRunOnUtc so next will be a week later
        var executedOn = schedule.NextRunOnUtc;
        schedule.RecordRun(executedOn);
        schedule.NextRunOnUtc.Should().BeAfter(executedOn);
        schedule.LastRunOnUtc.Should().NotBeNull();
    }
}

public class AlertRuleTests
{
    private static AlertCondition ConditionGt(decimal threshold) =>
        AlertCondition.Create(Comparator.GreaterThan, threshold).Value;

    [Fact]
    public void Create_EmptyChannels_Fails()
    {
        var result = AlertRule.Create("r", "api.latency.p95", ConditionGt(500), AlertSeverity.Critical,
            new List<AlertChannel>(), false);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AlertRule.NoChannels");
    }

    [Fact]
    public void Fire_EnabledRule_CreatesIncident()
    {
        var rule = AlertRule.Create("r", "api.latency.p95", ConditionGt(500), AlertSeverity.Critical,
            new List<AlertChannel> { AlertChannel.Email }, false).Value;
        rule.Fire(600, IncidentTrigger.ThresholdBreach, DateTime.UtcNow).IsSuccess.Should().BeTrue();
        rule.Incidents.Should().HaveCount(1);
    }

    [Fact]
    public void Fire_DisabledRule_Fails()
    {
        var rule = AlertRule.Create("r", "api.latency.p95", ConditionGt(500), AlertSeverity.Critical,
            new List<AlertChannel> { AlertChannel.Email }, false).Value;
        rule.Disable();
        var result = rule.Fire(600, IncidentTrigger.ThresholdBreach, DateTime.UtcNow);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AlertRule.Disabled");
    }

    [Fact]
    public void Fire_UnresolvedIncidentExists_NoDuplicateIncident()
    {
        var rule = AlertRule.Create("r", "api.latency.p95", ConditionGt(500), AlertSeverity.Critical,
            new List<AlertChannel> { AlertChannel.Email }, false).Value;
        rule.Fire(600, IncidentTrigger.ThresholdBreach, DateTime.UtcNow);
        rule.Fire(700, IncidentTrigger.ThresholdBreach, DateTime.UtcNow);
        rule.Incidents.Should().HaveCount(1);
    }

    [Fact]
    public void AcknowledgeIncident_RaisedIncident_Succeeds()
    {
        var rule = AlertRule.Create("r", "api.latency.p95", ConditionGt(500), AlertSeverity.Critical,
            new List<AlertChannel> { AlertChannel.Email }, false).Value;
        rule.Fire(600, IncidentTrigger.ThresholdBreach, DateTime.UtcNow);
        var incidentId = rule.Incidents.First().Id;
        rule.AcknowledgeIncident(incidentId, Guid.NewGuid()).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AcknowledgeIncident_NotFound_Fails()
    {
        var rule = AlertRule.Create("r", "api.latency.p95", ConditionGt(500), AlertSeverity.Critical,
            new List<AlertChannel> { AlertChannel.Email }, false).Value;
        rule.AcknowledgeIncident(Guid.NewGuid(), Guid.NewGuid()).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void EscalateIncident_RaisesEvent()
    {
        var rule = AlertRule.Create("r", "api.latency.p95", ConditionGt(500), AlertSeverity.Critical,
            new List<AlertChannel> { AlertChannel.Email }, false).Value;
        rule.Fire(600, IncidentTrigger.ThresholdBreach, DateTime.UtcNow);
        var incidentId = rule.Incidents.First().Id;
        rule.EscalateIncident(incidentId).IsSuccess.Should().BeTrue();
        rule.DomainEvents.Should().Contain(e => e.GetType().Name == "AlertIncidentEscalated");
    }
}
