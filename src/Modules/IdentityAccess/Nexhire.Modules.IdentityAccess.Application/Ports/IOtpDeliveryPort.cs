using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Ports;

public interface IOtpDeliveryPort
{
    Task<Result> SendAsync(string destination, string plaintextCode, OtpPurpose purpose, CancellationToken cancellationToken = default);
}
