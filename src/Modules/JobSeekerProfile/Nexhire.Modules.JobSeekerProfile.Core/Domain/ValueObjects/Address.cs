using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

public class Address : ValueObject
{
    public string Line1 { get; }
    public string? Line2 { get; }
    public string City { get; }
    public string District { get; }
    public string Postcode { get; }
    public string Country { get; }

    private Address(string line1, string? line2, string city, string district, string postcode, string country)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        District = district;
        Postcode = postcode;
        Country = country;
    }

    public static Result<Address> Create(string line1, string? line2, string city, string district, string postcode, string country)
    {
        if (string.IsNullOrWhiteSpace(line1))
        {
            return Result.Failure<Address>(new Error("Address.EmptyLine1", "Line 1 of address cannot be empty."));
        }
        if (string.IsNullOrWhiteSpace(city))
        {
            return Result.Failure<Address>(new Error("Address.EmptyCity", "City of address cannot be empty."));
        }
        if (string.IsNullOrWhiteSpace(district))
        {
            return Result.Failure<Address>(new Error("Address.EmptyDistrict", "District of address cannot be empty."));
        }
        if (string.IsNullOrWhiteSpace(postcode))
        {
            return Result.Failure<Address>(new Error("Address.EmptyPostcode", "Postcode of address cannot be empty."));
        }
        if (string.IsNullOrWhiteSpace(country))
        {
            return Result.Failure<Address>(new Error("Address.EmptyCountry", "Country of address cannot be empty."));
        }

        return Result.Success(new Address(
            line1.Trim(),
            line2?.Trim(),
            city.Trim(),
            district.Trim(),
            postcode.Trim(),
            country.Trim()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Line1;
        if (Line2 != null) yield return Line2;
        yield return City;
        yield return District;
        yield return Postcode;
        yield return Country;
    }
}
