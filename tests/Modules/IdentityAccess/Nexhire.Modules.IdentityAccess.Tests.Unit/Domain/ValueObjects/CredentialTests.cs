using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.ValueObjects;

public class CredentialTests
{
    public class Create
    {
        [Fact]
        public void Should_Succeed_When_All_Components_Are_Valid()
        {
            // Arrange
            var email = EmailAddress.Create("test@example.com").Value;
            var mobile = MobileNumber.Create("01712345678").Value;
            var passwordHash = PasswordHash.Create("$argon2id$v=19$m=65536$t=3,p=4$abc$xyz").Value;

            // Act
            var result = Credential.Create(email, mobile, passwordHash);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Email.Should().Be(email);
            result.Value.Mobile.Should().Be(mobile);
            result.Value.PasswordHash.Should().Be(passwordHash);
        }

        [Fact]
        public void Should_Fail_When_Email_Is_Null()
        {
            // Arrange
            EmailAddress? email = null;
            var mobile = MobileNumber.Create("01712345678").Value;
            var passwordHash = PasswordHash.Create("$argon2id$v=19$m=65536,t=3,p=4$abc$xyz").Value;

            // Act
            var result = Credential.Create(email!, mobile, passwordHash);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Credential.InvalidComponent");
        }

        [Fact]
        public void Should_Fail_When_Mobile_Is_Null()
        {
            // Arrange
            var email = EmailAddress.Create("test@example.com").Value;
            MobileNumber? mobile = null;
            var passwordHash = PasswordHash.Create("$argon2id$v=19$m=65536,t=3,p=4$abc$xyz").Value;

            // Act
            var result = Credential.Create(email, mobile!, passwordHash);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Credential.InvalidComponent");
        }

        [Fact]
        public void Should_Fail_When_PasswordHash_Is_Null()
        {
            // Arrange
            var email = EmailAddress.Create("test@example.com").Value;
            var mobile = MobileNumber.Create("01712345678").Value;
            PasswordHash? passwordHash = null;

            // Act
            var result = Credential.Create(email, mobile, passwordHash!);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Credential.InvalidComponent");
        }
    }
}
