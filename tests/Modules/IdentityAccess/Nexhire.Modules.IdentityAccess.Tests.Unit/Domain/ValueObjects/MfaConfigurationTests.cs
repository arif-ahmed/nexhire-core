using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.ValueObjects;

public class MfaConfigurationTests
{
    public class Create
    {
        [Fact]
        public void Should_Create_Disabled_Configuration_By_Default()
        {
            // Act
            var config = MfaConfiguration.CreateDisabled();

            // Assert
            config.Enabled.Should().BeFalse();
            config.Method.Should().Be(MfaMethod.None);
            config.SecretRef.Should().BeNull();
        }

        [Fact]
        public void Should_Create_Enabled_Totp_Configuration()
        {
            // Arrange
            var secretRef = "JBSWY3DPEHPK3PXP";

            // Act
            var result = MfaConfiguration.CreateEnabled(MfaMethod.Totp, secretRef);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Enabled.Should().BeTrue();
            result.Value.Method.Should().Be(MfaMethod.Totp);
            result.Value.SecretRef.Should().Be(secretRef);
        }

        [Fact]
        public void Should_Create_Enabled_SmsOtp_Configuration()
        {
            // Arrange
            var secretRef = "device-fingerprint-123";

            // Act
            var result = MfaConfiguration.CreateEnabled(MfaMethod.SmsOtp, secretRef);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Enabled.Should().BeTrue();
            result.Value.Method.Should().Be(MfaMethod.SmsOtp);
            result.Value.SecretRef.Should().Be(secretRef);
        }

        [Fact]
        public void Should_Fail_When_Method_Is_None_But_SecretRef_Is_Provided()
        {
            // Act
            var result = MfaConfiguration.CreateEnabled(MfaMethod.None, "some-secret");

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mfa.InvalidConfiguration");
        }

        [Fact]
        public void Should_Fail_When_Method_Is_Totp_But_SecretRef_Is_Empty()
        {
            // Act
            var result = MfaConfiguration.CreateEnabled(MfaMethod.Totp, "");

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mfa.InvalidConfiguration");
        }

        [Fact]
        public void Should_Fail_When_Method_Is_Totp_But_SecretRef_Is_Null()
        {
            // Act
            var result = MfaConfiguration.CreateEnabled(MfaMethod.Totp, null!);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mfa.InvalidConfiguration");
        }

        [Fact]
        public void Should_Fail_When_Method_Is_SmsOtp_But_SecretRef_Is_Empty()
        {
            // Act
            var result = MfaConfiguration.CreateEnabled(MfaMethod.SmsOtp, "");

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mfa.InvalidConfiguration");
        }

        [Fact]
        public void Should_Create_Disabled_From_Enabled()
        {
            // Arrange
            var enabled = MfaConfiguration.CreateEnabled(MfaMethod.Totp, "secret123").Value;

            // Act
            var disabled = enabled.Disable();

            // Assert
            disabled.Enabled.Should().BeFalse();
            disabled.Method.Should().Be(MfaMethod.None);
            disabled.SecretRef.Should().BeNull();
        }
    }
}