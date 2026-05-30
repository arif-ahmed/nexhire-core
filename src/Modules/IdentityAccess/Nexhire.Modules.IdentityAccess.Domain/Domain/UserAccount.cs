using Nexhire.Modules.IdentityAccess.Domain.Events;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.IdentityAccess.Domain;

public class UserAccount : AggregateRoot<Guid>
{
    public EmailAddress Email { get; private set; } = null!;
    public FullName FullName { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    private UserAccount(Guid id, EmailAddress email, FullName fullName) : base(id)
    {
        Email = email;
        FullName = fullName;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private UserAccount()
    {
    }

    public static UserAccount Create(Guid id, EmailAddress email, FullName fullName)
    {
        var user = new UserAccount(id, email, fullName);

        user.RaiseDomainEvent(new UserCreatedEvent(Guid.NewGuid(), user.Id, DateTime.UtcNow));

        return user;
    }
}
