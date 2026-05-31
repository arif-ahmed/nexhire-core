using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

public class Credential : ValueObject
{
    public EmailAddress  Email        { get; } = null!;
    public MobileNumber  Mobile       { get; } = null!;
    public PasswordHash  PasswordHash { get; } = null!;

    private Credential() { } // EF Core materialisation

    private Credential(EmailAddress email, MobileNumber mobile, PasswordHash passwordHash)
    {
        Email        = email;
        Mobile       = mobile;
        PasswordHash = passwordHash;
    }

    public static Result<Credential> Create(EmailAddress email, MobileNumber mobile, PasswordHash passwordHash)
        => new Credential(email, mobile, passwordHash);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email;
        yield return Mobile;
        yield return PasswordHash;
    }
}
