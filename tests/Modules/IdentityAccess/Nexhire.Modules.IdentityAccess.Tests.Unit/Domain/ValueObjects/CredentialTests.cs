using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.ValueObjects;

public class CredentialTests
{
    public class Create
    {
        [Fact]
        public void Should_Succeed_When_All_Components_Are_Valid()
        {
            var email        = EmailAddress.Create("test@example.com").Value;
            var mobile       = MobileNumber.Create("01712345678").Value;
            var passwordHash = PasswordHash.Create("$argon2id$v=19$m=65536$t=3,p=4$abc$xyz").Value;

            var result = Credential.Create(email, mobile, passwordHash);

            result.IsSuccess.Should().BeTrue();
            result.Value.Email.Should().Be(email);
            result.Value.Mobile.Should().Be(mobile);
            result.Value.PasswordHash.Should().Be(passwordHash);
        }

        [Fact]
        public void Should_Expose_All_Three_Components_Via_Properties()
        {
            var email        = EmailAddress.Create("cred@test.io").Value;
            var mobile       = MobileNumber.Create("+8801800000000").Value;
            var passwordHash = PasswordHash.Create("$argon2id$v=19$m=65536,t=3,p=4$abc$xyz").Value;

            var cred = Credential.Create(email, mobile, passwordHash).Value;

            cred.Email.Value.Should().Be("cred@test.io");
            cred.Mobile.Value.Should().NotBeNullOrEmpty();
            cred.PasswordHash.Algorithm.Should().Be("argon2id");
        }

        // NOTE (M-1): Null-guard tests removed. As of M-1, Credential.Create() accepts only
        // non-nullable parameters — the C# NRT compiler enforces this at call sites. Passing
        // null at runtime is a programming error, not a validation scenario.
    }
}
