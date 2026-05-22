using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.ValueObjects;

public class EmailAddressTests
{
    public class Create
    {
        [Fact]
        public void Should_Succeed_When_Email_Is_Valid()
        {
            // Arrange
            var email = "test@example.com";

            // Act
            var result = EmailAddress.Create(email);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(email.ToLowerInvariant());
        }

        [Fact]
        public void Should_Succeed_When_Email_Is_Valid_And_Already_Lowercase()
        {
            // Arrange
            var email = "alreadylowercase@example.com";

            // Act
            var result = EmailAddress.Create(email);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(email);
        }

        [Fact]
        public void Should_Fail_When_Email_Is_Empty()
        {
            // Arrange
            var email = "";

            // Act
            var result = EmailAddress.Create(email);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.Empty");
        }

        [Fact]
        public void Should_Fail_When_Email_Is_Null()
        {
            // Arrange
            string? email = null;

            // Act
            var result = EmailAddress.Create(email!);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.Empty");
        }

        [Fact]
        public void Should_Fail_When_Email_Is_Whitespace()
        {
            // Arrange
            var email = "   ";

            // Act
            var result = EmailAddress.Create(email);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.Empty");
        }

        [Fact]
        public void Should_Fail_When_Email_Does_Not_Contain_At_Sign()
        {
            // Arrange
            var email = "invalidemail.com";

            // Act
            var result = EmailAddress.Create(email);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.Invalid");
        }

        [Fact]
        public void Should_Succeed_When_Email_H_Valid_RFC_5322_Format()
        {
            // Arrange
            var email = "user.name+tag+sorting@example.co.uk";

            // Act
            var result = EmailAddress.Create(email);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(email.ToLowerInvariant());
        }

        [Fact]
        public void Should_Succeed_When_Email_Has_Dashes()
        {
            // Arrange
            var email = "first-last@example-domain.com";

            // Act
            var result = EmailAddress.Create(email);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Lowercase_Email_Domain()
        {
            // Arrange
            var email = "User@EXAMPLE.COM";

            // Act
            var result = EmailAddress.Create(email);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be("user@example.com");
        }
    }
}