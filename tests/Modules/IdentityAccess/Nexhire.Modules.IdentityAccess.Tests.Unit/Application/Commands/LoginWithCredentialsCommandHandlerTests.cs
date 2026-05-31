using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.LoginWithCredentials;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Application.Commands;

public class LoginWithCredentialsCommandHandlerTests
{
    // ── Port doubles ──────────────────────────────────────────────────────────

    private readonly IUserAccountRepository  _repo        = Substitute.For<IUserAccountRepository>();
    private readonly IOtpChallengeRepository _otpRepo     = Substitute.For<IOtpChallengeRepository>();
    private readonly IPasswordHasher         _hasher      = Substitute.For<IPasswordHasher>();
    private readonly IJwtSigner              _jwtSigner   = Substitute.For<IJwtSigner>();
    private readonly IRateLimiterPort        _rateLimiter = Substitute.For<IRateLimiterPort>();
    private readonly IOtpDeliveryPort        _otpDelivery = Substitute.For<IOtpDeliveryPort>();

    private LoginWithCredentialsCommandHandler CreateHandler() =>
        new(_repo, _otpRepo, _hasher, _jwtSigner, _rateLimiter, _otpDelivery);

    private static readonly LoginWithCredentialsCommand ValidCmd = new(
        "alice@example.com", "StrongP@ss1", "Web", "fp-001", "127.0.0.1");

    private UserAccount MakeActiveAccount()
    {
        var perms = PermissionResolver.Resolve(UserRole.JobSeeker, []);
        var account = UserAccount.Provision(
            EmailAddress.Create("alice@example.com").Value,
            MobileNumber.Create("+8801700000001").Value,
            PasswordHash.Create("$argon2id$stored-hash").Value,
            UserRole.JobSeeker,
            perms);
        account.Activate();
        account.ClearDomainEvents();
        return account;
    }

    private void SetupHappyPath(UserAccount account)
    {
        _rateLimiter.TryConsumeAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repo.GetByEmailOrMobileAsync(ValidCmd.Identifier, Arg.Any<CancellationToken>())
            .Returns(account);
        _hasher.Verify(Arg.Any<RawPassword>(), Arg.Any<PasswordHash>()).Returns(true);
        _jwtSigner.IssueRefreshToken().Returns(("refresh-token", "refresh-hash"));
        _jwtSigner.SignAccessToken(Arg.Any<AccessTokenSpec>()).Returns("access-token-jwt");
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Return_Tokens_On_Successful_Login()
    {
        var account = MakeActiveAccount();
        SetupHappyPath(account);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RequiresMfa.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Create_Session_On_Success()
    {
        var account = MakeActiveAccount();
        SetupHappyPath(account);
        var handler = CreateHandler();

        await handler.Handle(ValidCmd, CancellationToken.None);

        account.Sessions.Should().NotBeEmpty();
    }

    // ── Wrong password ────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Return_Generic_Error_For_Wrong_Password()
    {
        var account = MakeActiveAccount();
        SetupHappyPath(account);
        _hasher.Verify(Arg.Any<RawPassword>(), Arg.Any<PasswordHash>()).Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-LOGIN-INVALID-CREDENTIALS",
            because: "wrong password must not enumerate users");
    }

    [Fact]
    public async Task Should_Increment_FailedLoginCount_On_Wrong_Password()
    {
        var account = MakeActiveAccount();
        SetupHappyPath(account);
        _hasher.Verify(Arg.Any<RawPassword>(), Arg.Any<PasswordHash>()).Returns(false);
        var handler = CreateHandler();

        await handler.Handle(ValidCmd, CancellationToken.None);

        account.LockState.FailedLoginCount.Should().Be(1);
    }

    // ── Unknown user ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Return_Generic_Error_For_Unknown_Identifier()
    {
        _rateLimiter.TryConsumeAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repo.GetByEmailOrMobileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserAccount?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-LOGIN-INVALID-CREDENTIALS",
            because: "unknown user must return the same error as wrong password — no enumeration");
    }

    // ── Status-specific codes ─────────────────────────────────────────────────

    [Fact]
    public async Task Should_Return_Distinct_Code_For_PendingActivation()
    {
        // PendingActivation account — not yet activated
        var perms = PermissionResolver.Resolve(UserRole.JobSeeker, []);
        var pendingAccount = UserAccount.Provision(
            EmailAddress.Create("pending@example.com").Value,
            MobileNumber.Create("+8801700000002").Value,
            PasswordHash.Create("$argon2id$h").Value,
            UserRole.JobSeeker, perms);

        _rateLimiter.TryConsumeAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repo.GetByEmailOrMobileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(pendingAccount);
        _hasher.Verify(Arg.Any<RawPassword>(), Arg.Any<PasswordHash>()).Returns(true);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PENDING", because: "PendingActivation has its own error code");
    }

    [Fact]
    public async Task Should_Return_Distinct_Code_For_Suspended()
    {
        var account = MakeActiveAccount();
        account.Suspend("policy violation");

        _rateLimiter.TryConsumeAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repo.GetByEmailOrMobileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(account);
        _hasher.Verify(Arg.Any<RawPassword>(), Arg.Any<PasswordHash>()).Returns(true);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("SUSPENDED");
    }

    // ── Rate limiting ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Return_RATE_LIMITED_When_IP_Throttled()
    {
        _rateLimiter.TryConsumeAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("RATE-LIMITED");
    }

    // ── MFA path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Return_MFA_Challenge_When_MFA_Enabled()
    {
        var account = MakeActiveAccount();
        var hashes = Enumerable.Range(0, 8).Select(i => $"h{i}").ToList();
        account.EnableMfa(MfaMethod.SmsOtp, "secret", hashes);
        SetupHappyPath(account);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequiresMfa.Should().BeTrue("MFA-enabled accounts must not get tokens immediately");
        result.Value.AccessToken.Should().BeNullOrEmpty("no access token until MFA is verified");
    }

    // ── Remember-me TTL ──────────────────────────────────────────────────────��

    [Fact]
    public async Task Tokens_Are_Issued_On_Happy_Path_Without_MFA()
    {
        var account = MakeActiveAccount();
        SetupHappyPath(account);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token-jwt");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }
}
