using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Services;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.DomainServices;

public class SavedSearchMatcherTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly Guid EmployerId = Guid.NewGuid();

    private static JobIndexEntry MakeEntry(
        string title = "Developer",
        string[]? skills = null,
        string locationDistrict = "Dhaka",
        EmploymentType employmentType = EmploymentType.FullTime,
        WorkFormat workFormat = WorkFormat.Remote,
        decimal? salaryMin = null,
        decimal? salaryMax = null)
    {
        return JobIndexEntry.Project(
            Guid.NewGuid(), EmployerId, title, "Summary", "Corp",
            skills ?? [], null, null,
            locationDistrict, null, null, null,
            employmentType, workFormat,
            salaryMin, salaryMax, null, null,
            Now, null, 1, Now).Value;
    }

    [Fact]
    public void Matches_ShouldReturnTrue_WhenNoFiltersApplied()
    {
        var criteria = SearchCriteria.Create(keyword: "dev", allowEmptyForPersistence: false).Value;
        var entry = MakeEntry();

        SavedSearchMatcher.Matches(criteria, entry).Should().BeTrue();
    }

    [Fact]
    public void Matches_ShouldMatchSkillOverlap()
    {
        var filters = SearchFilters.Create(requiredSkills: ["C#", "SQL"]).Value;
        var criteria = SearchCriteria.Create(filters: filters).Value;
        var entry = MakeEntry(skills: ["C#", "SQL", "Azure"]);

        SavedSearchMatcher.Matches(criteria, entry).Should().BeTrue();
    }

    [Fact]
    public void Matches_ShouldNotMatch_WhenSkillsMissing()
    {
        var filters = SearchFilters.Create(requiredSkills: ["C#", "Java"]).Value;
        var criteria = SearchCriteria.Create(filters: filters).Value;
        var entry = MakeEntry(skills: ["C#", "SQL"]);

        SavedSearchMatcher.Matches(criteria, entry).Should().BeFalse();
    }

    [Fact]
    public void Matches_ShouldMatchEmploymentType()
    {
        var filters = SearchFilters.Create(employmentTypes: [EmploymentType.FullTime]).Value;
        var criteria = SearchCriteria.Create(filters: filters).Value;
        var entry = MakeEntry(employmentType: EmploymentType.FullTime);

        SavedSearchMatcher.Matches(criteria, entry).Should().BeTrue();
    }

    [Fact]
    public void Matches_ShouldNotMatch_WhenEmploymentTypeDifferent()
    {
        var filters = SearchFilters.Create(employmentTypes: [EmploymentType.FullTime]).Value;
        var criteria = SearchCriteria.Create(filters: filters).Value;
        var entry = MakeEntry(employmentType: EmploymentType.PartTime);

        SavedSearchMatcher.Matches(criteria, entry).Should().BeFalse();
    }

    [Fact]
    public void Matches_ShouldMatchLocation()
    {
        var location = GeoLocation.Create("Chittagong").Value;
        var filters = SearchFilters.Create(location: location).Value;
        var criteria = SearchCriteria.Create(filters: filters).Value;
        var entry = MakeEntry(locationDistrict: "Chittagong");

        SavedSearchMatcher.Matches(criteria, entry).Should().BeTrue();
    }

    [Fact]
    public void Matches_ShouldMatchSalaryRange()
    {
        var filters = SearchFilters.Create(salaryMin: 40000, salaryMax: 60000).Value;
        var criteria = SearchCriteria.Create(filters: filters).Value;
        var entry = MakeEntry(salaryMin: 45000, salaryMax: 55000);

        SavedSearchMatcher.Matches(criteria, entry).Should().BeTrue();
    }

    [Fact]
    public void Matches_ShouldUseANDLogicAcrossFilters()
    {
        var filters = SearchFilters.Create(
            employmentTypes: [EmploymentType.FullTime],
            workFormats: [WorkFormat.Remote]).Value;
        var criteria = SearchCriteria.Create(filters: filters).Value;

        var matching = MakeEntry(employmentType: EmploymentType.FullTime, workFormat: WorkFormat.Remote);
        var partial = MakeEntry(employmentType: EmploymentType.FullTime, workFormat: WorkFormat.OnSite);

        SavedSearchMatcher.Matches(criteria, matching).Should().BeTrue();
        SavedSearchMatcher.Matches(criteria, partial).Should().BeFalse();
    }

    [Fact]
    public void Matches_ShouldMatchWorkFormat()
    {
        var filters = SearchFilters.Create(workFormats: [WorkFormat.Hybrid]).Value;
        var criteria = SearchCriteria.Create(filters: filters).Value;
        var entry = MakeEntry(workFormat: WorkFormat.Hybrid);

        SavedSearchMatcher.Matches(criteria, entry).Should().BeTrue();
    }
}
