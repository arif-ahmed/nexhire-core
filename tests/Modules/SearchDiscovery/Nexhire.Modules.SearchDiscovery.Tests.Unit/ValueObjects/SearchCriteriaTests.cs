using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.ValueObjects;

public class SearchCriteriaTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenKeywordProvided()
    {
        var result = SearchCriteria.Create(keyword: "developer");

        result.IsSuccess.Should().BeTrue();
        result.Value.Keyword.Should().Be("developer");
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public void Create_ShouldSucceed_WhenFiltersProvided()
    {
        var filters = SearchFilters.Create(employmentTypes: [EmploymentType.FullTime]).Value;
        var result = SearchCriteria.Create(filters: filters);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSucceed_WhenIntentHintProvided()
    {
        var hint = IntentHint.Create(skillTerms: ["C#"]).Value;
        var result = SearchCriteria.Create(intentHint: hint);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenNoCriteriaProvided()
    {
        var result = SearchCriteria.Create();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SearchCriteria.EmptyCriteria");
    }

    [Fact]
    public void Create_ShouldFail_WhenPageZero()
    {
        var result = SearchCriteria.Create(keyword: "dev", page: 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SearchCriteria.InvalidPage");
    }

    [Fact]
    public void Create_ShouldFail_WhenPageSizeZero()
    {
        var result = SearchCriteria.Create(keyword: "dev", pageSize: 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SearchCriteria.InvalidPageSize");
    }

    [Fact]
    public void Create_ShouldFail_WhenPageSizeExceeds100()
    {
        var result = SearchCriteria.Create(keyword: "dev", pageSize: 101);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SearchCriteria.InvalidPageSize");
    }

    [Fact]
    public void Create_ShouldSucceed_WhenFilterOnly_ForPersistence()
    {
        var emptyFilters = SearchFilters.Create().Value;
        var result = SearchCriteria.Create(filters: emptyFilters, allowEmptyForPersistence: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRespectSortOption()
    {
        var result = SearchCriteria.Create(keyword: "dev", sort: SortOption.DatePosted);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sort.Should().Be(SortOption.DatePosted);
    }
}
