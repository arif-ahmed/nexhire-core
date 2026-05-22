using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Core.Domain.ValueObjects;

public class AccessTokenSpec : ValueObject
{
    public Guid Subject { get; }
    public string Role { get; }
    public IReadOnlyList<string> Permissions { get; }
    public IReadOnlyList<string> Scopes { get; }
    public Guid SessionId { get; }
    public DateTime ExpiresOnUtc { get; }

    private AccessTokenSpec(
        Guid subject,
        string role,
        IReadOnlyList<string> permissions,
        IReadOnlyList<string> scopes,
        Guid sessionId,
        DateTime expiresOnUtc)
    {
        Subject = subject;
        Role = role;
        Permissions = permissions;
        Scopes = scopes;
        SessionId = sessionId;
        ExpiresOnUtc = expiresOnUtc;
    }

    public static Result<AccessTokenSpec> Create(
        Guid subject,
        string role,
        IReadOnlyList<string> permissions,
        IReadOnlyList<string> scopes,
        Guid sessionId,
        DateTime expiresOnUtc)
    {
        var maxTtl = TimeSpan.FromHours(1);
        var ttl = expiresOnUtc - DateTime.UtcNow;

        if (ttl <= TimeSpan.Zero)
            return Result.Failure<AccessTokenSpec>(new Error("AccessTokenSpec.InvalidExpiry", "Access token expiry must be in the future."));

        if (ttl > maxTtl)
            return Result.Failure<AccessTokenSpec>(new Error("AccessTokenSpec.TtlTooLong", "Access token TTL must not exceed 1 hour."));

        return new AccessTokenSpec(
            subject,
            role,
            permissions.ToList().AsReadOnly(),
            scopes.ToList().AsReadOnly(),
            sessionId,
            expiresOnUtc);
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresOnUtc;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Subject;
        yield return Role;
        yield return string.Join(",", Permissions);
        yield return string.Join(",", Scopes);
        yield return SessionId;
        yield return ExpiresOnUtc;
    }
}