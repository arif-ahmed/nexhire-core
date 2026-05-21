using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

namespace Nexhire.Modules.EmployerProfiles.Core.DTOs;

public record PublicEmployerProfileDto(
    Guid Id,
    string CompanyName,
    string? Website,
    string? Industry,
    string? CompanySize,
    AddressDto? Address,
    string? Description,
    FileReference? Logo);
