using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UpdateEmployerProfile;

public record UpdateEmployerProfileCommand(
    Guid UserId,
    string? CompanyName,
    string? Website,
    string? Industry,
    string? CompanySize,
    AddressDto? Address,
    string? Description) : ICommand;
