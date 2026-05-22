using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.ValueObjects;

public class DeviceFingerprintTests
{
    public class Create
    {
        [Fact]
        public void Should_Succeed_When_Fingerprint_Is_Valid()
        {
            // Arrange
            var fingerprint = "abc123def456";

            // Act
            var result = DeviceFingerprint.Create(fingerprint);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(fingerprint);
        }

        [Fact]
        public void Should_Fail_When_Fingerprint_Is_Empty()
        {
            // Arrange
            var fingerprint = "";

            // Act
            var result = DeviceFingerprint.Create(fingerprint);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("DeviceFingerprint.Empty");
        }

        [Fact]
        public void Should_Fail_When_Fingerprint_Is_Null()
        {
            // Arrange
            string? fingerprint = null;

            // Act
            var result = DeviceFingerprint.Create(fingerprint!);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("DeviceFingerprint.Empty");
        }

        [Fact]
        public void Should_Fail_When_Fingerprint_Is_Whitespace()
        {
            // Arrange
            var fingerprint = "   ";

            // Act
            var result = DeviceFingerprint.Create(fingerprint);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("DeviceFingerprint.Empty");
        }

        [Fact]
        public void Should_Trim_Whitespace_From_Valid_Fingerprint()
        {
            // Arrange
            var fingerprint = "  abc123def456  ";

            // Act
            var result = DeviceFingerprint.Create(fingerprint);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be("abc123def456");
        }

        [Fact]
        public void Should_Ensure_Equality_Based_On_Value()
        {
            // Arrange
            var fingerprint1 = DeviceFingerprint.Create("abc123").Value;
            var fingerprint2 = DeviceFingerprint.Create("abc123").Value;
            var fingerprint3 = DeviceFingerprint.Create("xyz789").Value;

            // Assert
            fingerprint1.Should().Be(fingerprint2);
            fingerprint1.Should().NotBe(fingerprint3);
        }
    }
}