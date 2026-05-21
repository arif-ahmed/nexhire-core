using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.CompleteEmployerLevel2;

public record CompleteEmployerLevel2Command(
    Guid UserId,
    string Website,
    string Industry,
    string CompanySize,
    AddressDto Address,
    string Description) : ICommand;
