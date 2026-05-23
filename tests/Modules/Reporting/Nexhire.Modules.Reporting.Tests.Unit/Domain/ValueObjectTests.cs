using FluentAssertions;
using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;

namespace Nexhire.Modules.Reporting.Tests.Unit.Domain;

public class ValueObjectTests
{
    [Fact]
    public void EmailAddress_ValidEmail_Succeeds()
    {
        var result = EmailAddress.Create("Test@Example.Com");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("test@example.com"); // lower-cased
    }

    [Fact]
    public void EmailAddress_InvalidEmail_Fails()
    {
        var result = EmailAddress.Create("not-an-email");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Invalid");
    }

    [Fact]
    public void EmailAddress_Empty_Fails()
    {
        var result = EmailAddress.Create("");
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DateRange_StartAfterEnd_Fails()
    {
        var result = DateRange.Create(DateTime.UtcNow.AddDays(1), DateTime.UtcNow);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DateRange.Invalid");
    }

    [Fact]
    public void DateRange_StartEqualsEnd_Succeeds()
    {
        var dt = DateTime.UtcNow;
        var result = DateRange.Create(dt, dt);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FileReference_NegativeSize_Fails()
    {
        var result = FileReference.Create("key", "file.pdf", "application/pdf", -1);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FileReference.InvalidSize");
    }

    [Fact]
    public void FileReference_ValidInputs_Succeeds()
    {
        var result = FileReference.Create("storage/key", "file.pdf", "application/pdf", 1024);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ReportSpec_EmptyMetrics_Fails()
    {
        var result = ReportSpec.Create(new List<string>(), new List<string>(), new List<ReportFilter>(), VisualizationType.Table,
            _ => true, _ => true);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ReportSpec.NoMetrics");
    }

    [Fact]
    public void ReportSpec_UnknownMetric_Fails()
    {
        var result = ReportSpec.Create(new List<string> { "unknown.metric" }, new List<string>(), new List<ReportFilter>(), VisualizationType.Table,
            _ => false, _ => true);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ReportSpec.UnknownMetric");
    }

    [Fact]
    public void ReportSpec_ValidMetrics_Succeeds()
    {
        var result = ReportSpec.Create(new List<string> { "posting.volume" }, new List<string>(), new List<ReportFilter>(), VisualizationType.BarChart,
            _ => true, _ => true);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ReportFilter_BetweenWithoutTwoValues_Fails()
    {
        var result = ReportFilter.Create("date", FilterOperator.Between, new List<string> { "2024-01-01" });
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ReportFilter.BetweenValues");
    }

    [Fact]
    public void ReportFilter_BetweenWithTwoValues_Succeeds()
    {
        var result = ReportFilter.Create("date", FilterOperator.Between, new List<string> { "2024-01-01", "2024-12-31" });
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ScheduleCadence_WeeklyWithoutDayOfWeek_Fails()
    {
        var result = ScheduleCadence.Create(Frequency.Weekly, null, null, new TimeOnly(9, 0), new List<DateOnly>());
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ScheduleCadence.MissingDayOfWeek");
    }

    [Fact]
    public void ScheduleCadence_MonthlyWithInvalidDay_Fails()
    {
        var result = ScheduleCadence.Create(Frequency.Monthly, null, 29, new TimeOnly(9, 0), new List<DateOnly>());
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ScheduleCadence.InvalidDayOfMonth");
    }

    [Fact]
    public void ScheduleCadence_WeeklyWithDayOfWeek_Succeeds()
    {
        var result = ScheduleCadence.Create(Frequency.Weekly, DayOfWeek.Monday, null, new TimeOnly(9, 0), new List<DateOnly>());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RoleScope_EmployerOwnerWithoutEmployerId_Fails()
    {
        var result = RoleScope.Create(RoleName.EmployerOwner, null);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoleScope.MissingEmployerId");
    }

    [Fact]
    public void RoleScope_EmployerOwnerWithEmployerId_Succeeds()
    {
        var result = RoleScope.Create(RoleName.EmployerOwner, Guid.NewGuid());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ReportVisibility_EmptyRoles_Fails()
    {
        var result = ReportVisibility.Create(new HashSet<string>());
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ReportVisibility.Empty");
    }

    [Fact]
    public void ConfigurableParameter_EmptyName_Fails()
    {
        var result = ConfigurableParameter.Create("", ParameterKind.DateRange, true, null);
        result.IsFailure.Should().BeTrue();
    }
}
