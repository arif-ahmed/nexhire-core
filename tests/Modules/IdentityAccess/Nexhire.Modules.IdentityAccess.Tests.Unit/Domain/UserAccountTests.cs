using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Modules.IdentityAccess.Contracts.Events;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Events;
using System.Linq;
using Xunit;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain;

public class UserAccountTests
{
    private static UserAccount CreateTestAccount()
    {
        return UserAccount.Provision(
            EmailAddress.Create("test@example.com").Value,
            MobileNumber.Create("+1234567890").Value,
            PasswordHash.Create("$argon2id$hashed-password").Value,
            UserRole.JobSeeker,
            new[] { "apply-job" }
        );
    }

    [Fact]
    public void Provision_Should_Create_Account_And_Raise_UserRegisteredIntegrationEvent()
    {
        // Act
        var account = CreateTestAccount();

        // Assert
        account.Should().NotBeNull();
        account.Status.Should().Be(AccountStatus.PendingActivation);
        account.Role.Should().Be(UserRole.JobSeeker);
        account.Permissions.Should().Contain("apply-job");
        account.IdentityVerified.Should().BeFalse();
        account.LockState.IsLocked.Should().BeFalse();

        var domainEvents = account.DomainEvents.ToList();
        domainEvents.Should().ContainSingle(e => e is UserRegisteredIntegrationEvent);
        var regEvent = (UserRegisteredIntegrationEvent)domainEvents.First(e => e is UserRegisteredIntegrationEvent);
        regEvent.Email.Should().Be("test@example.com");
        regEvent.Role.Should().Be(UserRole.JobSeeker.ToString());
    }

    [Fact]
    public void Activate_Should_Transition_To_Active_When_PendingActivation()
    {
        // Arrange
        var account = CreateTestAccount();

        // Act
        var result = account.Activate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(AccountStatus.Active);
        account.ActivatedOnUtc.Should().NotBeNull();

        var domainEvents = account.DomainEvents.ToList();
        domainEvents.Should().Contain(e => e is UserAccountActivatedIntegrationEvent);
    }

    [Fact]
    public void Activate_Should_Return_Failure_When_Already_Active()
    {
        // Arrange
        var account = CreateTestAccount();
        account.Activate(); // First time works

        // Act
        var result = account.Activate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Account.InvalidTransition");
    }

    [Fact]
    public void Suspend_Should_Transition_To_Suspended_And_Raise_Event()
    {
        // Arrange
        var account = CreateTestAccount();
        account.Activate();
        
        // Clear previous events for cleaner assertions
        account.ClearDomainEvents();

        // Act
        var result = account.Suspend("Violation of TOS");

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(AccountStatus.Suspended);
        account.SuspendedReason.Should().Be("Violation of TOS");

        var domainEvents = account.DomainEvents.ToList();
        domainEvents.Should().ContainSingle(e => e is UserAccountSuspendedIntegrationEvent);
    }

    [Fact]
    public void RecordOtpFailure_Should_Increment_FailedOtpCount()
    {
        // Arrange
        var account = CreateTestAccount();
        account.Activate();
        
        // Act
        account.RecordOtpFailure();
        account.RecordOtpFailure();

        // Assert
        account.LockState.FailedOtpCount.Should().Be(2);
    }
    
    [Fact]
    public void Unlock_Should_Reset_Failed_Counts()
    {
        // Arrange
        var account = CreateTestAccount();
        account.Activate();
        
        account.RecordOtpFailure();
        account.LockState.FailedOtpCount.Should().Be(1);

        // Act
        account.Unlock();

        // Assert
        account.LockState.IsLocked.Should().BeFalse();
        account.LockState.FailedOtpCount.Should().Be(0);
        account.DomainEvents.Should().Contain(e => e is AccountUnlockedEvent);
    }
}
