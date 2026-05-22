using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.ValueObjects;

public class PasswordHashTests
{
    public class Create
    {
        [Fact]
        public void Should_Succeed_When_Hash_Is_Valid()
        {
            // Arrange
            var hashValue = "$argon2id$v=19$m=65536,t=3,p=4$abc123$xyz789";

            // Act
            var result = PasswordHash.Create(hashValue);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(hashValue);
            result.Value.Algorithm.Should().Be("argon2id");
        }

        [Fact]
        public void Should_Fail_When_Hash_Is_Empty()
        {
            // Arrange
            var hashValue = "";

            // Act
            var result = PasswordHash.Create(hashValue);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("PasswordHash.Empty");
        }

        [Fact]
        public void Should_Fail_When_Hash_Is_Null()
        {
            // Arrange
            string? hashValue = null;

            // Act
            var result = PasswordHash.Create(hashValue!);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("PasswordHash.Empty");
        }

        [Fact]
        public void Should_Fail_When_Hash_Is_Whitespace()
        {
            // Arrange
            var hashValue = "   ";

            // Act
            var result = PasswordHash.Create(hashValue);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("PasswordHash.Empty");
        }

        [Fact]
        public void Should_Fail_When_Algorithm_Is_Not_Argon2id()
        {
            // Arrange
            var hashValue = "$pbkdf2$v=19$m=10000$abc$xyz";

            // Act
            var result = PasswordHash.Create(hashValue);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("PasswordHash.InvalidAlgorithm");
        }

        [Fact]
        public void Should_Fail_When_Hash_Has_No_Algorithm_Prefix()
        {
            // Arrange
            var hashValue = "randomhashvalue123";

            // Act
            var result = PasswordHash.Create(hashValue);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("PasswordHash.InvalidAlgorithm");
        }
    }
}