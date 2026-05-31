using Nexhire.Modules.EmployerProfiles.Domain.ValueObjects;

namespace Nexhire.Modules.EmployerProfiles.Application.DTOs;

public record PublicEmployerProfileDto(
    Guid Id,
    string CompanyName,
    string? Website,
    string? Industry,
    string? CompanySize,
    AddressDto? Address,
    string? Description,
    FileReference? Logo);
