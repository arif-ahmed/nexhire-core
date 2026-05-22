using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Services;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.DomainServices;

public class SearchCriteriaInterpreterTests
{
    [Fact]
    public void Apply_ShouldFoldIntentHintWorkFormatIntoFilter()
    {
        var baseCriteria = SearchCriteria.Create(keyword: "developer").Value;
        var hint = IntentHint.Create(workFormat: WorkFormat.Remote).Value;

        var result = SearchCriteriaInterpreter.Apply(baseCriteria, hint);

        result.Filters.WorkFormats.Should().Contain(WorkFormat.Remote);
    }

    [Fact]
    public void Apply_ShouldFoldIntentHintSkillsIntoFilter()
    {
        var baseCriteria = SearchCriteria.Create(keyword: "developer").Value;
        var hint = IntentHint.Create(skillTerms: ["C#", "Azure"]).Value;

        var result = SearchCriteriaInterpreter.Apply(baseCriteria, hint);

        result.Filters.RequiredSkills.Should().BeEquivalentTo(["C#", "Azure"]);
    }

    [Fact]
    public void Apply_ShouldFoldIntentHintLocationIntoFilter()
    {
        var baseCriteria = SearchCriteria.Create(keyword: "developer").Value;
        var hint = IntentHint.Create(locationTerm: "Dhaka").Value;

        var result = SearchCriteriaInterpreter.Apply(baseCriteria, hint);

        result.Filters.Location.Should().NotBeNull();
        result.Filters.Location!.District.Should().Be("Dhaka");
    }

    [Fact]
    public void Apply_ShouldReturnOriginalCriteria_WhenNoHint()
    {
        var baseCriteria = SearchCriteria.Create(keyword: "developer").Value;

        var result = SearchCriteriaInterpreter.Apply(baseCriteria, null);

        result.Keyword.Should().Be("developer");
        result.Filters.HasAnyFilter.Should().BeFalse();
    }

    [Fact]
    public void Apply_ShouldMergeWithExistingFilters()
    {
        var existingFilters = SearchFilters.Create(employmentTypes: [EmploymentType.FullTime]).Value;
        var baseCriteria = SearchCriteria.Create(keyword: "dev", filters: existingFilters).Value;
        var hint = IntentHint.Create(workFormat: WorkFormat.Remote).Value;

        var result = SearchCriteriaInterpreter.Apply(baseCriteria, hint);

        result.Filters.EmploymentTypes.Should().Contain(EmploymentType.FullTime);
        result.Filters.WorkFormats.Should().Contain(WorkFormat.Remote);
    }
}
