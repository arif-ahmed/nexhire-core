using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.CompletePasswordReset;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Application.Commands;

public class CompletePasswordResetCommandHandlerTests
{
    private readonly IUserAccountRepository _repo    = Substitute.For<IUserAccountRepository>();
    private readonly IPasswordHasher        _hasher  = Substitute.For<IPasswordHasher>();
    private readonly IBreachCheckPort       _breach  = Substitute.For<IBreachCheckPort>();

    private CompletePasswordResetCommandHandler CreateHandler() =>
        new(_repo, _hasher, _breach);

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static UserAccount MakeActiveAccountWithToken(string rawToken, out string storedTokenHash)
    {
        var perms = PermissionResolver.Resolve(UserRole.JobSeeker, []);
        var account = UserAccount.Provision(
            EmailAddress.Create("user@example.com").Value,
            MobileNumber.Create("+8801700000001").Value,
            PasswordHash.Create("$argon2id$old-hash").Value,
            UserRole.JobSeeker, perms);
        account.Activate();

        storedTokenHash = HashToken(rawToken);
        account.IssuePasswordResetToken(storedTokenHash, DateTime.UtcNow.AddHours(1));
        account.ClearDomainEvents();
        return account;
    }

    private static readonly CompletePasswordResetCommand ValidCmd = new(
        "user@example.com", "my-reset-token", "NewStrongP@ss9");

    private void SetupHappyPath(UserAccount account)
    {
        _repo.GetByPasswordResetTokenHashAsync(HashToken(ValidCmd.ResetToken), Arg.Any<CancellationToken>())
            .Returns(account);
        _breach.IsBreachedAsync(Arg.Any<RawPassword>(), Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash(Arg.Any<RawPassword>()).Returns(PasswordHash.Create("$argon2id$new-hash").Value);
    }

    // ── Happy path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Update_Password_And_Revoke_Sessions_On_Valid_Token()
    {
        var account = MakeActiveAccountWithToken(ValidCmd.ResetToken, out _);
        // Add an active session
        var fp = DeviceFingerprint.Create("fp-1").Value;
        account.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));
        SetupHappyPath(account);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Sessions.Should().AllSatisfy(s => s.IsRevoked.Should().BeTrue());
    }

    // ── Invalid / not-found token ──────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_RESET_INVALID_TOKEN_When_Token_Not_Found()
    {
        _repo.GetByPasswordResetTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserAccount?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-RESET-INVALID-TOKEN");
    }

    // ── Breached new password ──────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_BREACHED_PASSWORD_When_New_Password_Is_Breached()
    {
        var account = MakeActiveAccountWithToken(ValidCmd.ResetToken, out _);
        SetupHappyPath(account);
        _breach.IsBreachedAsync(Arg.Any<RawPassword>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("BREACHED");
    }

    // ── Reused password ────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_PASSWORD_REUSED_When_Password_In_History()
    {
        var account = MakeActiveAccountWithToken(ValidCmd.ResetToken, out _);
        account.AddToPasswordHistory(ValidCmd.NewPassword); // pre-seed history
        SetupHappyPath(account);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("REUSED");
    }

    // ── Weak new password ──────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_When_New_Password_Too_Short()
    {
        var account = MakeActiveAccountWithToken(ValidCmd.ResetToken, out _);
        _repo.GetByPasswordResetTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _breach.IsBreachedAsync(Arg.Any<RawPassword>(), Arg.Any<CancellationToken>()).Returns(false);
        var cmd = ValidCmd with { NewPassword = "Ab1!" }; // too short
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PASSWORD");
    }

    // ── No-enumeration ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Return_Same_Error_For_Invalid_Token_Regardless_Of_Whether_Account_Exists()
    {
        _repo.GetByPasswordResetTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserAccount?)null);
        var handler = CreateHandler();

        var result1 = await handler.Handle(ValidCmd, CancellationToken.None);

        result1.Error.Code.Should().Be("E-RESET-INVALID-TOKEN",
            because: "we must not enumerate whether the account exists");
    }
}
