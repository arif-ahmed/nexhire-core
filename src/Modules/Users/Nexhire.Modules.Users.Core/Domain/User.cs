using Nexhire.Modules.Users.Core.Domain.Events;
using Nexhire.Modules.Users.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.Users.Core.Domain;

public class User : AggregateRoot<Guid>
{
    public Email Email { get; private set; } = null!;
    public FullName FullName { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    private User(Guid id, Email email, FullName fullName) : base(id)
    {
        Email = email;
        FullName = fullName;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private User()
    {
        // Required by EF Core
    }

    public static User Create(Guid id, Email email, FullName fullName)
    {
        var user = new User(id, email, fullName);

        user.RaiseDomainEvent(new UserCreatedEvent(Guid.NewGuid(), user.Id, DateTime.UtcNow));

        return user;
    }
}
