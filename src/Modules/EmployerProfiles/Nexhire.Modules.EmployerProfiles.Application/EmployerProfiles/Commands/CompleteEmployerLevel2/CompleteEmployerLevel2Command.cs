using Nexhire.Modules.EmployerProfiles.Application.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.CompleteEmployerLevel2;

public record CompleteEmployerLevel2Command(
    Guid UserId,
    string Website,
    string Industry,
    string CompanySize,
    AddressDto Address,
    string Description) : ICommand;
