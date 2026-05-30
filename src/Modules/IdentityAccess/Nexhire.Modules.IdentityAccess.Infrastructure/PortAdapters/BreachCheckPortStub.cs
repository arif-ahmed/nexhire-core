using Nexhire.Modules.IdentityAccess.Application.Ports;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.PortAdapters;

public class BreachCheckPortStub : IBreachCheckPort
{
    private static readonly HashSet<string> _breachedPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "123456",
        "qwerty",
        "admin123"
    };

    public Task<bool> IsBreachedAsync(Nexhire.Modules.IdentityAccess.Domain.ValueObjects.RawPassword password, CancellationToken cancellationToken = default)
    {
        // Simple stub implementation
        return Task.FromResult(_breachedPasswords.Contains(password.Value));
    }
}
