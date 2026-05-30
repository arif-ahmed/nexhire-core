using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.ValueObjects;

public class AccessTokenSpecTests
{
    public class Create
    {
        [Fact]
        public void Should_Succeed_When_Spec_Is_Valid()
        {
            // Arrange
            var subject = Guid.NewGuid();
            var role = "Employer";
            var permissions = new[] { "jobs:write", "candidates:read" };
            var scopes = new[] { "read", "write" };
            var sessionId = Guid.NewGuid();
            var expiresOnUtc = DateTime.UtcNow.AddHours(1);

            // Act
            var result = AccessTokenSpec.Create(subject, role, permissions, scopes, sessionId, expiresOnUtc);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Subject.Should().Be(subject);
            result.Value.Role.Should().Be(role);
            result.Value.Permissions.Should().BeEquivalentTo(permissions);
            result.Value.Scopes.Should().BeEquivalentTo(scopes);
            result.Value.SessionId.Should().Be(sessionId);
            result.Value.ExpiresOnUtc.Should().Be(expiresOnUtc);
        }

        [Fact]
        public void Should_Fail_When_TTL_Exceeds_One_Hour()
        {
            // Arrange
            var subject = Guid.NewGuid();
            var expiresOnUtc = DateTime.UtcNow.AddHours(2); // Exceeds 1 hour

            // Act
            var result = AccessTokenSpec.Create(
                subject, "Employer", 
                Array.Empty<string>(), 
                Array.Empty<string>(), 
                Guid.NewGuid(), 
                expiresOnUtc);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("AccessTokenSpec.TtlTooLong");
        }

        [Fact]
        public void Should_Fail_When_ExpiresOnUtc_Is_In_Past()
        {
            // Arrange
            var subject = Guid.NewGuid();
            var expiresOnUtc = DateTime.UtcNow.AddMinutes(-5);

            // Act
            var result = AccessTokenSpec.Create(
                subject, "Employer", 
                Array.Empty<string>(), 
                Array.Empty<string>(), 
                Guid.NewGuid(), 
                expiresOnUtc);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("AccessTokenSpec.InvalidExpiry");
        }

        [Fact]
        public void Should_Allow_Exactly_One_Hour_TTL()
        {
            // Arrange
            var subject = Guid.NewGuid();
            var expiresOnUtc = DateTime.UtcNow.AddHours(1);

            // Act
            var result = AccessTokenSpec.Create(
                subject, "Employer", 
                Array.Empty<string>(), 
                Array.Empty<string>(), 
                Guid.NewGuid(), 
                expiresOnUtc);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Succeed_With_Empty_Permissions_And_Scopes()
        {
            // Arrange
            var subject = Guid.NewGuid();
            var expiresOnUtc = DateTime.UtcNow.AddMinutes(30);

            // Act
            var result = AccessTokenSpec.Create(
                subject, "Employer", 
                Array.Empty<string>(), 
                Array.Empty<string>(), 
                Guid.NewGuid(), 
                expiresOnUtc);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Allow_Claims_Collection_As_ReadOnly()
        {
            // Arrange
            var subject = Guid.NewGuid();
            var expiresOnUtc = DateTime.UtcNow.AddMinutes(30);
            var permissions = new[] { "jobs:write" };

            // Act
            var result = AccessTokenSpec.Create(
                subject, "Employer", 
                permissions, 
                Array.Empty<string>(), 
                Guid.NewGuid(), 
                expiresOnUtc);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var spec = result.Value;
            spec.Permissions.Should().BeEquivalentTo(permissions);
        }
    }
}
