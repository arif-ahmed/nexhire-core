using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.ValueObjects;

public class SalaryRangeTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenValid()
    {
        var result = SalaryRange.Create(50000, 80000);

        result.IsSuccess.Should().BeTrue();
        result.Value.Min.Should().Be(50000);
        result.Value.Max.Should().Be(80000);
        result.Value.Currency.Should().Be("BDT");
    }

    [Fact]
    public void Create_ShouldSucceed_WithCustomCurrency()
    {
        var result = SalaryRange.Create(1000, 2000, "USD");

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_ShouldFail_WhenMinNegative()
    {
        var result = SalaryRange.Create(-1, 100);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SalaryRange.NegativeMin");
    }

    [Fact]
    public void Create_ShouldFail_WhenMinGreaterThanMax()
    {
        var result = SalaryRange.Create(200, 100);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SalaryRange.MinExceedsMax");
    }

    [Fact]
    public void Create_ShouldSucceed_WhenMinEqualsMax()
    {
        var result = SalaryRange.Create(50000, 50000);

        result.IsSuccess.Should().BeTrue();
    }
}
