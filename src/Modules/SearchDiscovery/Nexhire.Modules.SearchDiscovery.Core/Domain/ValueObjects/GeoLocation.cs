using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

public class GeoLocation : ValueObject
{
    public string District { get; }
    public string? City { get; }
    public double? Latitude { get; }
    public double? Longitude { get; }

    private GeoLocation(string district, string? city, double? latitude, double? longitude)
    {
        District = district;
        City = city;
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Result<GeoLocation> Create(
        string district,
        string? city = null,
        double? latitude = null,
        double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(district))
            return Result.Failure<GeoLocation>(new Error("GeoLocation.EmptyDistrict", "District is required."));

        if (latitude.HasValue != longitude.HasValue)
            return Result.Failure<GeoLocation>(new Error("GeoLocation.PartialCoordinates", "Latitude and longitude must both be provided or both absent."));

        if (latitude.HasValue && (latitude < -90 || latitude > 90))
            return Result.Failure<GeoLocation>(new Error("GeoLocation.LatitudeOutOfRange", "Latitude must be between -90 and 90."));

        if (longitude.HasValue && (longitude < -180 || longitude > 180))
            return Result.Failure<GeoLocation>(new Error("GeoLocation.LongitudeOutOfRange", "Longitude must be between -180 and 180."));

        return Result.Success(new GeoLocation(district.Trim(), city?.Trim(), latitude, longitude));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return District;
        yield return City ?? string.Empty;
        yield return Latitude ?? 0;
        yield return Longitude ?? 0;
    }
}
