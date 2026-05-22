using FluentAssertions;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Tests.Unit.ValueObjects;

public class GeoLocationTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenValidDistrictOnly()
    {
        var result = GeoLocation.Create("Dhaka");

        result.IsSuccess.Should().BeTrue();
        result.Value.District.Should().Be("Dhaka");
        result.Value.City.Should().BeNull();
        result.Value.Latitude.Should().BeNull();
        result.Value.Longitude.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSucceed_WhenAllFieldsProvided()
    {
        var result = GeoLocation.Create("Dhaka", "Gulshan", 23.7935, 90.4143);

        result.IsSuccess.Should().BeTrue();
        result.Value.District.Should().Be("Dhaka");
        result.Value.City.Should().Be("Gulshan");
        result.Value.Latitude.Should().Be(23.7935);
        result.Value.Longitude.Should().Be(90.4143);
    }

    [Fact]
    public void Create_ShouldFail_WhenDistrictEmpty()
    {
        var result = GeoLocation.Create("");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GeoLocation.EmptyDistrict");
    }

    [Fact]
    public void Create_ShouldFail_WhenLatitudeWithoutLongitude()
    {
        var result = GeoLocation.Create("Dhaka", null, 23.7935, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GeoLocation.PartialCoordinates");
    }

    [Fact]
    public void Create_ShouldFail_WhenLongitudeWithoutLatitude()
    {
        var result = GeoLocation.Create("Dhaka", null, null, 90.4143);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GeoLocation.PartialCoordinates");
    }

    [Fact]
    public void Create_ShouldFail_WhenLatitudeOutOfRange()
    {
        var result = GeoLocation.Create("Dhaka", null, 91.0, 90.0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GeoLocation.LatitudeOutOfRange");
    }

    [Fact]
    public void Create_ShouldFail_WhenLongitudeOutOfRange()
    {
        var result = GeoLocation.Create("Dhaka", null, 23.0, 181.0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GeoLocation.LongitudeOutOfRange");
    }

    [Fact]
    public void GetEqualityComponents_ShouldReturnSameValues()
    {
        var a = GeoLocation.Create("Dhaka", "Gulshan", 23.0, 90.0).Value;
        var b = GeoLocation.Create("Dhaka", "Gulshan", 23.0, 90.0).Value;

        a.Should().Be(b);
    }
}
