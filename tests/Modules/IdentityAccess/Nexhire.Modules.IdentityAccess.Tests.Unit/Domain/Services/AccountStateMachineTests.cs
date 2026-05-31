using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.Services;

/// <summary>
/// Exhaustive 4×4 transition matrix for AccountStateMachine (spec §6.2 invariant #1).
/// Legal transitions (7): PA→Active, PA→Suspended, Active→Suspended, Suspended→Active,
///                         Active→Deactivated, Deactivated→PA, Deactivated→Active
/// Illegal transitions (9): all remaining pairs including self-transitions
/// </summary>
public class AccountStateMachineTests
{
    // ── Legal transitions ────────────────────────────────────────────────────

    [Theory]
    [InlineData(AccountStatus.PendingActivation, AccountStatus.Active)]
    [InlineData(AccountStatus.PendingActivation, AccountStatus.Suspended)]
    [InlineData(AccountStatus.Active,            AccountStatus.Suspended)]
    [InlineData(AccountStatus.Suspended,         AccountStatus.Active)]
    [InlineData(AccountStatus.Active,            AccountStatus.Deactivated)]
    [InlineData(AccountStatus.Deactivated,       AccountStatus.PendingActivation)]
    [InlineData(AccountStatus.Deactivated,       AccountStatus.Active)]
    public void Should_Succeed_For_Legal_Transition(AccountStatus from, AccountStatus to)
    {
        var result = AccountStateMachine.EnsureTransitionAllowed(from, to);

        result.IsSuccess.Should().BeTrue(
            because: $"{from} → {to} is a legal transition");
    }

    // ── Illegal transitions ──────────────────────────────────────────────────

    [Theory]
    [InlineData(AccountStatus.PendingActivation, AccountStatus.Deactivated)]  // spec explicitly forbids
    [InlineData(AccountStatus.Active,            AccountStatus.PendingActivation)]
    [InlineData(AccountStatus.Suspended,         AccountStatus.Deactivated)]  // spec explicitly forbids
    [InlineData(AccountStatus.Suspended,         AccountStatus.Suspended)]    // self
    [InlineData(AccountStatus.Suspended,         AccountStatus.PendingActivation)]
    [InlineData(AccountStatus.Deactivated,       AccountStatus.Suspended)]    // spec explicitly forbids
    [InlineData(AccountStatus.Deactivated,       AccountStatus.Deactivated)]  // self
    [InlineData(AccountStatus.Active,            AccountStatus.Active)]       // self
    [InlineData(AccountStatus.PendingActivation, AccountStatus.PendingActivation)] // self
    public void Should_Fail_For_Illegal_Transition(AccountStatus from, AccountStatus to)
    {
        var result = AccountStateMachine.EnsureTransitionAllowed(from, to);

        result.IsFailure.Should().BeTrue(
            because: $"{from} → {to} is an illegal transition");
        result.Error.Code.Should().Be("Account.InvalidTransition");
    }

    // ── Error message content ────────────────────────────────────────────────

    [Fact]
    public void Should_Include_From_And_To_Status_In_Error_Message()
    {
        var result = AccountStateMachine.EnsureTransitionAllowed(
            AccountStatus.Suspended, AccountStatus.Deactivated);

        result.Error.Message.Should().Contain("Suspended");
        result.Error.Message.Should().Contain("Deactivated");
    }
}
