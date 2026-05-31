namespace Nexhire.Modules.EmployerProfiles.Application.DTOs;

public record AddressDto(
    string Line1,
    string? Line2,
    string City,
    string District,
    string Postcode,
    string Country);
