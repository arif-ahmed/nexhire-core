using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.Services;

public class TokenClaimsBuilderTests
{
    private static UserAccount MakeAccount(UserRole role = UserRole.JobSeeker)
    {
        var permissions = PermissionResolver.Resolve(role, []);
        return UserAccount.Provision(
            EmailAddress.Create("claims@example.com").Value,
            MobileNumber.Create("+8801700000001").Value,
            PasswordHash.Create("$argon2id$stub-hash").Value,
            role,
            permissions);
    }

    public class BuildAccessToken
    {
        [Fact]
        public void Should_Succeed_For_Valid_Inputs()
        {
            var account   = MakeAccount();
            var sessionId = Guid.NewGuid();

            var result = TokenClaimsBuilder.BuildAccessToken(
                account, sessionId, ["jobs:read"], TimeSpan.FromMinutes(30));

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Set_Subject_To_AccountId()
        {
            var account   = MakeAccount();
            var sessionId = Guid.NewGuid();

            var spec = TokenClaimsBuilder.BuildAccessToken(
                account, sessionId, [], TimeSpan.FromMinutes(30)).Value;

            spec.Subject.Should().Be(account.Id.Value);
        }

        [Fact]
        public void Should_Set_Role_From_Account()
        {
            var account   = MakeAccount(UserRole.Employer);
            var sessionId = Guid.NewGuid();

            var spec = TokenClaimsBuilder.BuildAccessToken(
                account, sessionId, [], TimeSpan.FromMinutes(30)).Value;

            spec.Role.Should().Be("Employer");
        }

        [Fact]
        public void Should_Include_Permissions_From_Account()
        {
            var account   = MakeAccount(UserRole.JobSeeker);
            var sessionId = Guid.NewGuid();

            var spec = TokenClaimsBuilder.BuildAccessToken(
                account, sessionId, [], TimeSpan.FromMinutes(30)).Value;

            spec.Permissions.Should().Contain("profile:self");
            spec.Permissions.Should().Contain("applications:self");
            spec.Permissions.Should().Contain("search:read");
        }

        [Fact]
        public void Should_Include_Requested_Scopes()
        {
            var account   = MakeAccount();
            var sessionId = Guid.NewGuid();
            var scopes    = new[] { "jobs:read", "applications:write" };

            var spec = TokenClaimsBuilder.BuildAccessToken(
                account, sessionId, scopes, TimeSpan.FromMinutes(30)).Value;

            spec.Scopes.Should().BeEquivalentTo(scopes);
        }

        [Fact]
        public void Should_Set_SessionId_Correctly()
        {
            var account   = MakeAccount();
            var sessionId = Guid.NewGuid();

            var spec = TokenClaimsBuilder.BuildAccessToken(
                account, sessionId, [], TimeSpan.FromMinutes(30)).Value;

            spec.SessionId.Should().Be(sessionId);
        }

        [Fact]
        public void Should_Set_Expiry_Based_On_Ttl()
        {
            var account   = MakeAccount();
            var sessionId = Guid.NewGuid();
            var ttl       = TimeSpan.FromMinutes(45);
            var before    = DateTime.UtcNow.Add(ttl).AddSeconds(-2);
            var after     = DateTime.UtcNow.Add(ttl).AddSeconds(2);

            var spec = TokenClaimsBuilder.BuildAccessToken(account, sessionId, [], ttl).Value;

            spec.ExpiresOnUtc.Should().BeAfter(before).And.BeBefore(after);
        }

        [Fact]
        public void Should_Fail_When_Ttl_Exceeds_One_Hour()
        {
            var account   = MakeAccount();
            var sessionId = Guid.NewGuid();

            var result = TokenClaimsBuilder.BuildAccessToken(
                account, sessionId, [], TimeSpan.FromHours(2));

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("AccessTokenSpec.TtlTooLong");
        }

        [Fact]
        public void Spec_Should_Not_Contain_Password_Hash_Or_Secret_Fields()
        {
            var account   = MakeAccount();
            var sessionId = Guid.NewGuid();

            var spec = TokenClaimsBuilder.BuildAccessToken(
                account, sessionId, [], TimeSpan.FromMinutes(30)).Value;

            // Verify the spec carries no sensitive credential data
            spec.Should().NotBeNull();
            // AccessTokenSpec only exposes: Subject, Role, Permissions, Scopes, SessionId, ExpiresOnUtc
            // None of these is a password hash or secret — asserted by checking the type surface
            var props = typeof(AccessTokenSpec).GetProperties().Select(p => p.Name);
            props.Should().NotContain(p =>
                p.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("Secret",   StringComparison.OrdinalIgnoreCase) ||
                p.Contains("Hash",     StringComparison.OrdinalIgnoreCase));
        }
    }
}
