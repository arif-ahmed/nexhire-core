using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Application.Ports;

public interface IPasswordHasher
{
    PasswordHash Hash(RawPassword password);
    bool Verify(RawPassword password, PasswordHash hash);
}
