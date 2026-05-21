using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

public class Address : ValueObject
{
    public string Line1 { get; }
    public string? Line2 { get; }
    public string City { get; }
    public string District { get; }
    public string? Postcode { get; }
    public string Country { get; }

    private Address(string line1, string? line2, string city, string district, string? postcode, string country)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        District = district;
        Postcode = postcode;
        Country = country;
    }

    public static Result<Address> Create(string line1, string? line2, string city, string district, string? postcode, string country)
    {
        if (string.IsNullOrWhiteSpace(line1) || 
            string.IsNullOrWhiteSpace(city) || 
            string.IsNullOrWhiteSpace(district) || 
            string.IsNullOrWhiteSpace(country))
        {
            return Result.Failure<Address>(new Error("Address.RequiredFieldsMissing", "Address line 1, city, district, and country are required."));
        }

        return Result.Success(new Address(
            line1.Trim(),
            line2?.Trim(),
            city.Trim(),
            district.Trim(),
            postcode?.Trim(),
            country.Trim()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Line1;
        if (Line2 != null) yield return Line2;
        yield return City;
        yield return District;
        if (Postcode != null) yield return Postcode;
        yield return Country;
    }
}
