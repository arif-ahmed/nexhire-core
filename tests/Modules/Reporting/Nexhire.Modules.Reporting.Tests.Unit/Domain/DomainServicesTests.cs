using FluentAssertions;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Services;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;

namespace Nexhire.Modules.Reporting.Tests.Unit.Domain;

public class MetricCatalogTests
{
    [Fact]
    public void IsKnownMetric_KnownKey_ReturnsTrue()
    {
        MetricCatalog.IsKnownMetric("posting.volume").Should().BeTrue();
        MetricCatalog.IsKnownMetric("api.latency.p95").Should().BeTrue();
    }

    [Fact]
    public void IsKnownMetric_UnknownKey_ReturnsFalse()
    {
        MetricCatalog.IsKnownMetric("unknown.metric").Should().BeFalse();
    }

    [Fact]
    public void All_ReturnsNonEmptyCollection()
    {
        MetricCatalog.All().Should().NotBeEmpty();
    }
}

public class RetentionCutoffCalculatorTests
{
    [Theory]
    [InlineData(30, 30)]
    [InlineData(90, 90)]
    [InlineData(365, 365)]
    public void ComputeCutoff_SubtractsDays(int retentionDays, int expectedDaysBack)
    {
        var policy = BuildPolicy(retentionDays, 7);
        var now = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var cutoff = RetentionCutoffCalculator.ComputeCutoff(policy, now);
        cutoff.Should().Be(now.AddDays(-expectedDaysBack));
    }

    [Fact]
    public void ComputeWarningDate_IsAfterCutoff()
    {
        var policy = BuildPolicy(90, 7);
        var now = DateTime.UtcNow;
        var cutoff = RetentionCutoffCalculator.ComputeCutoff(policy, now);
        var warning = RetentionCutoffCalculator.ComputeWarningDate(policy, now);
        warning.Should().BeAfter(cutoff);
    }

    private static Nexhire.Modules.Reporting.Core.Domain.Aggregates.RetentionPolicy BuildPolicy(int retentionDays, int warningDays)
    {
        var scope = RetentionScope.Create(ActorRole.JobSeeker, new HashSet<ActivityType> { ActivityType.Login });
        return Nexhire.Modules.Reporting.Core.Domain.Aggregates.RetentionPolicy.Create(
            "Policy", scope, retentionDays, RetentionAction.Archive, warningDays).Value;
    }
}

public class ScheduleNextRunCalculatorTests
{
    [Fact]
    public void Daily_NextRunIsNextDay()
    {
        var cadence = ScheduleCadence.Create(Frequency.Daily, null, null, new TimeOnly(9, 0), new List<DateOnly>()).Value;
        var now = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var next = ScheduleNextRunCalculator.ComputeNextRun(cadence, now);
        next.Date.Should().Be(now.Date.AddDays(1));
        next.Hour.Should().Be(9);
    }

    [Fact]
    public void Weekly_NextRunIsCorrectWeekday()
    {
        var cadence = ScheduleCadence.Create(Frequency.Weekly, DayOfWeek.Friday, null, new TimeOnly(8, 0), new List<DateOnly>()).Value;
        var now = new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc); // Monday
        var next = ScheduleNextRunCalculator.ComputeNextRun(cadence, now);
        next.DayOfWeek.Should().Be(DayOfWeek.Friday);
        next.Should().BeAfter(now);
    }

    [Fact]
    public void Weekly_SkipDate_SkipsToNextOccurrence()
    {
        var skipDate = DateOnly.FromDateTime(new DateTime(2025, 6, 6)); // Friday
        var cadence = ScheduleCadence.Create(Frequency.Weekly, DayOfWeek.Friday, null, new TimeOnly(8, 0),
            new List<DateOnly> { skipDate }).Value;
        var now = new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc);
        var next = ScheduleNextRunCalculator.ComputeNextRun(cadence, now);
        next.Date.Should().NotBe(new DateTime(2025, 6, 6));
    }

    [Fact]
    public void Monthly_NextRunIsInFuture()
    {
        var cadence = ScheduleCadence.Create(Frequency.Monthly, null, 15, new TimeOnly(8, 0), new List<DateOnly>()).Value;
        var now = new DateTime(2025, 6, 20, 0, 0, 0, DateTimeKind.Utc); // after the 15th
        var next = ScheduleNextRunCalculator.ComputeNextRun(cadence, now);
        next.Should().BeAfter(now);
    }
}

public class AnomalyDetectorTests
{
    [Fact]
    public void Evaluate_ZeroStdDev_NotAnomaly()
    {
        var baseline = new BaselineWindow(100, 0, 10);
        var result = AnomalyDetector.Evaluate("metric", 200, baseline);
        result.IsAnomaly.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_3SigmaExceeded_IsAnomaly()
    {
        var baseline = new BaselineWindow(100, 10, 50);
        var result = AnomalyDetector.Evaluate("metric", 140, baseline); // 4 sigma
        result.IsAnomaly.Should().BeTrue();
        result.DeviationSigma.Should().BeGreaterThan(3);
    }

    [Fact]
    public void Evaluate_Under3Sigma_NotAnomaly()
    {
        var baseline = new BaselineWindow(100, 10, 50);
        var result = AnomalyDetector.Evaluate("metric", 110, baseline); // 1 sigma
        result.IsAnomaly.Should().BeFalse();
    }
}

public class ActivityClassifierTests
{
    [Fact]
    public void Classify_SearchPerformed_SeekerHint_IsJobSearch()
    {
        var result = ActivityClassifier.Classify("SearchPerformedIntegrationEvent", "JobSeeker");
        result.IsActivity.Should().BeTrue();
        result.ActivityType.Should().Be(ActivityType.JobSearch);
        result.ActorRole.Should().Be(ActorRole.JobSeeker);
    }

    [Fact]
    public void Classify_SearchPerformed_EmployerHint_IsCandidateSearch()
    {
        var result = ActivityClassifier.Classify("SearchPerformedIntegrationEvent", "Employer");
        result.IsActivity.Should().BeTrue();
        result.ActivityType.Should().Be(ActivityType.CandidateSearch);
        result.ActorRole.Should().Be(ActorRole.Employer);
    }

    [Fact]
    public void Classify_UnknownEventType_NotActivity()
    {
        var result = ActivityClassifier.Classify("UnknownEvent");
        result.IsActivity.Should().BeFalse();
        result.ActivityType.Should().BeNull();
    }

    [Fact]
    public void Classify_UserLoggedIn_IsLoginActivity()
    {
        var result = ActivityClassifier.Classify("UserLoggedInIntegrationEvent");
        result.IsActivity.Should().BeTrue();
        result.ActivityType.Should().Be(ActivityType.Login);
    }
}

public class ReportDataScopeFilterTests
{
    private static ReportSpec SimpleSpec() =>
        ReportSpec.Create(new List<string> { "posting.volume" }, new List<string>(), new List<ReportFilter>(),
            VisualizationType.Table, _ => true, _ => true).Value;

    [Fact]
    public void MaskedFieldsFor_SystemAdmin_NoMaskedFields()
    {
        var scope = RoleScope.Create(RoleName.SystemAdministrator, null).Value;
        var masked = ReportDataScopeFilter.MaskedFieldsFor(scope, ReportCategory.EmploymentStats);
        masked.Should().BeEmpty();
    }

    [Fact]
    public void MaskedFieldsFor_EmployerOwner_MasksFields()
    {
        var scope = RoleScope.Create(RoleName.EmployerOwner, Guid.NewGuid()).Value;
        var masked = ReportDataScopeFilter.MaskedFieldsFor(scope, ReportCategory.Custom);
        masked.Should().Contain("salary_min");
        masked.Should().Contain("email");
    }

    [Fact]
    public void ApplyRoleScope_EmployerOwner_InjectsEmployerFilter()
    {
        var employerId = Guid.NewGuid();
        var scope = RoleScope.Create(RoleName.EmployerOwner, employerId).Value;
        var filterSet = ReportDataScopeFilter.ApplyRoleScope(SimpleSpec(), scope);
        filterSet.Filters.Should().Contain(f => f.Field == "employer_id");
    }

    [Fact]
    public void ApplyRoleScope_SystemAdmin_NoExtraFilters()
    {
        var scope = RoleScope.Create(RoleName.SystemAdministrator, null).Value;
        var filterSet = ReportDataScopeFilter.ApplyRoleScope(SimpleSpec(), scope);
        filterSet.Filters.Should().NotContain(f => f.Field == "employer_id");
    }
}
