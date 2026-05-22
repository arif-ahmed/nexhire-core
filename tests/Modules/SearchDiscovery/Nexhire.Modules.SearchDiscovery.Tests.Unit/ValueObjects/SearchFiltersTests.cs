using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.ValueObjects;

public class SearchFiltersTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenNoFiltersProvided()
    {
        var result = SearchFilters.Create();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSucceed_WhenSalaryRangeValid()
    {
        var result = SearchFilters.Create(salaryMin: 50000, salaryMax: 80000);

        result.IsSuccess.Should().BeTrue();
        result.Value.SalaryMin.Should().Be(50000);
        result.Value.SalaryMax.Should().Be(80000);
    }

    [Fact]
    public void Create_ShouldFail_WhenSalaryMinExceedsMax()
    {
        var result = SearchFilters.Create(salaryMin: 80000, salaryMax: 50000);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SearchFilters.InvalidSalaryRange");
    }

    [Fact]
    public void Create_ShouldFail_WhenDateFromExceedsDateTo()
    {
        var from = new DateTime(2026, 6, 1);
        var to = new DateTime(2026, 5, 1);
        var result = SearchFilters.Create(datePostedFrom: from, datePostedTo: to);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SearchFilters.InvalidDateRange");
    }

    [Fact]
    public void Create_ShouldFail_WhenRadiusWithoutLocation()
    {
        var result = SearchFilters.Create(radiusKm: 50);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SearchFilters.RadiusWithoutLocation");
    }

    [Fact]
    public void Create_ShouldSucceed_WhenRadiusWithLocation()
    {
        var location = GeoLocation.Create("Dhaka").Value;
        var result = SearchFilters.Create(location: location, radiusKm: 50);

        result.IsSuccess.Should().BeTrue();
        result.Value.RadiusKm.Should().Be(50);
    }

    [Fact]
    public void Create_ShouldSucceed_WithEmploymentTypes()
    {
        var result = SearchFilters.Create(employmentTypes: [EmploymentType.FullTime, EmploymentType.PartTime]);

        result.IsSuccess.Should().BeTrue();
        result.Value.EmploymentTypes.Should().BeEquivalentTo([EmploymentType.FullTime, EmploymentType.PartTime]);
    }

    [Fact]
    public void Create_ShouldSucceed_WithWorkFormats()
    {
        var result = SearchFilters.Create(workFormats: [WorkFormat.Remote]);

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkFormats.Should().BeEquivalentTo([WorkFormat.Remote]);
    }
}
