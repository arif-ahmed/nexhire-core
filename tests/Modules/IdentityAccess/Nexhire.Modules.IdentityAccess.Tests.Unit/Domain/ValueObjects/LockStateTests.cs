using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.ValueObjects;

public class LockStateTests
{
    public class Create
    {
        [Fact]
        public void Should_Create_Unlocked_State_By_Default()
        {
            // Act
            var lockState = LockState.CreateUnlocked();

            // Assert
            lockState.IsLocked.Should().BeFalse();
            lockState.LockedUntilUtc.Should().BeNull();
            lockState.FailedLoginCount.Should().Be(0);
            lockState.FailedOtpCount.Should().Be(0);
        }

        [Fact]
        public void Should_Create_Locked_State_With_Expiry()
        {
            // Arrange
            var lockUntil = DateTime.UtcNow.AddMinutes(15);

            // Act
            var lockState = LockState.CreateLocked(lockUntil, 5, 2);

            // Assert
            lockState.IsLocked.Should().BeTrue();
            lockState.LockedUntilUtc.Should().Be(lockUntil);
            lockState.FailedLoginCount.Should().Be(5);
            lockState.FailedOtpCount.Should().Be(2);
        }

        [Fact]
        public void Should_Be_Expired_When_LockedUntil_Is_In_Past()
        {
            // Arrange
            var lockUntil = DateTime.UtcNow.AddMinutes(-1);
            var lockState = LockState.CreateLocked(lockUntil, 5, 2);

            // Act
            var isExpired = lockState.IsExpired();

            // Assert
            isExpired.Should().BeTrue();
        }

        [Fact]
        public void Should_Not_Be_Expired_When_LockedUntil_Is_In_Future()
        {
            // Arrange
            var lockUntil = DateTime.UtcNow.AddMinutes(15);
            var lockState = LockState.CreateLocked(lockUntil, 5, 2);

            // Act
            var isExpired = lockState.IsExpired();

            // Assert
            isExpired.Should().BeFalse();
        }

        [Fact]
        public void Should_Increment_FailedLoginCount()
        {
            // Arrange
            var lockState = LockState.CreateUnlocked();

            // Act
            var updated = lockState.IncrementFailedLogin();

            // Assert
            updated.FailedLoginCount.Should().Be(1);
            updated.FailedOtpCount.Should().Be(0);
            updated.IsLocked.Should().BeFalse();
        }

        [Fact]
        public void Should_Increment_FailedOtpCount()
        {
            // Arrange
            var lockState = LockState.CreateUnlocked();

            // Act
            var updated = lockState.IncrementFailedOtp();

            // Assert
            updated.FailedLoginCount.Should().Be(0);
            updated.FailedOtpCount.Should().Be(1);
            updated.IsLocked.Should().BeFalse();
        }

        [Fact]
        public void Should_Reset_All_Counters_When_Unlocked()
        {
            // Arrange
            var lockUntil = DateTime.UtcNow.AddMinutes(15);
            var lockState = LockState.CreateLocked(lockUntil, 5, 2);

            // Act
            var unlocked = lockState.Unlock();

            // Assert
            unlocked.IsLocked.Should().BeFalse();
            unlocked.LockedUntilUtc.Should().BeNull();
            unlocked.FailedLoginCount.Should().Be(0);
            unlocked.FailedOtpCount.Should().Be(0);
        }

        [Fact]
        public void Should_Validate_Counters_Are_Non_Negative()
        {
            // Arrange
            var lockState = LockState.CreateLocked(DateTime.UtcNow.AddMinutes(15), -1, 0);

            // Act & Assert
            lockState.FailedLoginCount.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
