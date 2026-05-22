using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Core.Domain.ValueObjects;

public class Credential : ValueObject
{
    public EmailAddress Email { get; }
    public MobileNumber Mobile { get; }
    public PasswordHash PasswordHash { get; }

    private Credential(EmailAddress email, MobileNumber mobile, PasswordHash passwordHash)
    {
        Email = email;
        Mobile = mobile;
        PasswordHash = passwordHash;
    }

    public static Result<Credential> Create(EmailAddress? email, MobileNumber? mobile, PasswordHash? passwordHash)
    {
        if (email == null || mobile == null || passwordHash == null)
            return Result.Failure<Credential>(new Error("Credential.InvalidComponent", "All credential components must be provided."));

        return new Credential(email, mobile, passwordHash);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email;
        yield return Mobile;
        yield return PasswordHash;
    }
}