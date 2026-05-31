using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ActivateAccount;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Application.Commands;

public class ActivateAccountCommandHandlerTests
{
    private readonly IUserAccountRepository  _repo    = Substitute.For<IUserAccountRepository>();
    private readonly IOtpChallengeRepository _otpRepo = Substitute.For<IOtpChallengeRepository>();

    private ActivateAccountCommandHandler CreateHandler() =>
        new(_repo, _otpRepo);

    private static UserAccount MakePendingAccount()
    {
        var perms = PermissionResolver.Resolve(UserRole.JobSeeker, []);
        return UserAccount.Provision(
            EmailAddress.Create("user@example.com").Value,
            MobileNumber.Create("+8801700000001").Value,
            PasswordHash.Create("$argon2id$h").Value,
            UserRole.JobSeeker, perms);
    }

    private static OtpChallenge MakeChallenge(UserAccountId accountId, string codeHash = "correct")
    {
        return OtpChallenge.Issue(accountId, OtpPurpose.Activation, codeHash, TimeSpan.FromMinutes(5), 5);
    }

    // ── Happy path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Activate_Account_When_OTP_Is_Correct()
    {
        var account = MakePendingAccount();
        var challenge = MakeChallenge(account.Id);
        _repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _otpRepo.GetActiveByAccountAndPurposeAsync(account.Id, OtpPurpose.Activation, Arg.Any<CancellationToken>())
            .Returns(challenge);
        var cmd = new ActivateAccountCommand(account.Id.Value, "correct");
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(AccountStatus.Active);
    }

    // ── Expired OTP ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_OTP_EXPIRED_When_Challenge_Is_Past_Ttl()
    {
        var account = MakePendingAccount();
        // Issue a challenge with a 1-second TTL that has already expired
        var expiredChallenge = OtpChallenge.Issue(
            account.Id, OtpPurpose.Activation, "correct", TimeSpan.FromSeconds(1), 5);

        _repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _otpRepo.GetActiveByAccountAndPurposeAsync(account.Id, OtpPurpose.Activation, Arg.Any<CancellationToken>())
            .Returns(expiredChallenge);

        var cmd = new ActivateAccountCommand(account.Id.Value, "correct");
        var handler = CreateHandler();

        // Simulate time passing — the handler calls Verify(now) which will detect expiry
        // We can't easily freeze time, so we test via the challenge directly:
        // Manually expire by sending with a future time via the domain model test
        // Here we test that the handler propagates the OTP failure correctly:
        expiredChallenge.MarkExpired(); // force expired state

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-OTP-EXPIRED");
    }

    // ── Too many failed OTPs ───────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_And_Record_Otp_Failure_When_Challenge_Locks()
    {
        var account = MakePendingAccount();
        var challenge = MakeChallenge(account.Id);
        // Exhaust attempts
        var now = DateTime.UtcNow;
        for (var i = 0; i < 4; i++) challenge.Verify("wrong", now);

        _repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _otpRepo.GetActiveByAccountAndPurposeAsync(account.Id, OtpPurpose.Activation, Arg.Any<CancellationToken>())
            .Returns(challenge);

        var cmd = new ActivateAccountCommand(account.Id.Value, "wrong"); // 5th wrong = locked
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-OTP-LOCKED");
        account.LockState.FailedOtpCount.Should().Be(1, "handler records OtpFailure on lock");
    }

    // ── Account not found ──────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_When_Account_Not_Found()
    {
        _repo.GetByIdAsync(Arg.Any<UserAccountId>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        var cmd = new ActivateAccountCommand(Guid.NewGuid(), "123456");
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-NOT-FOUND");
    }
}
