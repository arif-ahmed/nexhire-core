using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Application.Ports;

public interface IBreachCheckPort
{
    Task<bool> IsBreachedAsync(RawPassword password, CancellationToken cancellationToken = default);
}
