using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetAddresses;

public record SetAddressesCommand(
    Guid UserId,
    string CurrentLine1,
    string? CurrentLine2,
    string CurrentCity,
    string CurrentDistrict,
    string CurrentPostcode,
    string CurrentCountry,
    string? PermanentLine1 = null,
    string? PermanentLine2 = null,
    string? PermanentCity = null,
    string? PermanentDistrict = null,
    string? PermanentPostcode = null,
    string? PermanentCountry = null) : ICommand;
