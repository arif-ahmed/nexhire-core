using Microsoft.Extensions.Logging;
using Nexhire.Modules.IdentityAccess.Application.Ports;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.PortAdapters;

public class OtpDeliveryPortStub : IOtpDeliveryPort
{
    private readonly ILogger<OtpDeliveryPortStub> _logger;

    public OtpDeliveryPortStub(ILogger<OtpDeliveryPortStub> logger)
    {
        _logger = logger;
    }

    public Task<Nexhire.Shared.Core.Results.Result> SendAsync(string destination, string plaintextCode, Nexhire.Modules.IdentityAccess.Domain.Domain.OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OTP for {Purpose} to {Destination}: {Code}", purpose, destination, plaintextCode);
        return Task.FromResult(Nexhire.Shared.Core.Results.Result.Success());
    }
}
