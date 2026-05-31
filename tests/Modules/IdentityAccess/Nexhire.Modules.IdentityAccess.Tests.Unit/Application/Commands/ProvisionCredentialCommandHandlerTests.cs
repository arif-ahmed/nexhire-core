using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ProvisionCredential;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Application.Commands;

public class ProvisionCredentialCommandHandlerTests
{
    // ── Port doubles ──────────────────────────────────────────────────────────

    private readonly IUserAccountRepository _repo        = Substitute.For<IUserAccountRepository>();
    private readonly IOtpChallengeRepository _otpRepo     = Substitute.For<IOtpChallengeRepository>();
    private readonly IPasswordHasher         _hasher      = Substitute.For<IPasswordHasher>();
    private readonly IBreachCheckPort        _breach      = Substitute.For<IBreachCheckPort>();
    private readonly IRateLimiterPort        _rateLimiter = Substitute.For<IRateLimiterPort>();
    private readonly IOtpDeliveryPort        _otpDelivery = Substitute.For<IOtpDeliveryPort>();

    private ProvisionCredentialCommandHandler CreateHandler() =>
        new(_repo, _otpRepo, _hasher, _breach, _rateLimiter, _otpDelivery);

    private static readonly ProvisionCredentialCommand ValidCommand = new(
        "alice@example.com", "+8801700000001", "StrongP@ss1", "JobSeeker");

    private void SetupHappyPath()
    {
        _rateLimiter.TryConsumeAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.MobileExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _breach.IsBreachedAsync(Arg.Any<RawPassword>(), Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash(Arg.Any<RawPassword>()).Returns(PasswordHash.Create("$argon2id$hashed").Value);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Return_UserId_On_Success()
    {
        SetupHappyPath();
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Should_Persist_Account_And_OtpChallenge_On_Success()
    {
        SetupHappyPath();
        var handler = CreateHandler();

        await handler.Handle(ValidCommand, CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
        await _otpRepo.Received(1).AddAsync(Arg.Any<OtpChallenge>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Call_Hasher_Exactly_Once_On_Happy_Path()
    {
        SetupHappyPath();
        var handler = CreateHandler();

        await handler.Handle(ValidCommand, CancellationToken.None);

        _hasher.Received(1).Hash(Arg.Any<RawPassword>());
    }

    [Fact]
    public async Task Should_Send_Activation_Otp_On_Success()
    {
        SetupHappyPath();
        var handler = CreateHandler();

        await handler.Handle(ValidCommand, CancellationToken.None);

        await _otpDelivery.Received(1).SendAsync(
            ValidCommand.Mobile, Arg.Any<string>(), OtpPurpose.Activation, Arg.Any<CancellationToken>());
    }

    // ── Duplicate email ───────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_E_REG_DUPLICATE_EMAIL_When_Email_Exists()
    {
        SetupHappyPath();
        _repo.EmailExistsAsync(ValidCommand.Email, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("DUPLICATE");
        await _repo.DidNotReceive().AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
    }

    // ── Duplicate mobile ──────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_Duplicate_Error_When_Mobile_Exists()
    {
        SetupHappyPath();
        _repo.MobileExistsAsync(ValidCommand.Mobile, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("DUPLICATE");
        await _repo.DidNotReceive().AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
    }

    // ── Breached password ─────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_BREACHED_When_Password_Is_Breached()
    {
        SetupHappyPath();
        _breach.IsBreachedAsync(Arg.Any<RawPassword>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("BREACHED");
        await _repo.DidNotReceive().AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
    }

    // ── Rate limited ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_RATE_LIMITED_When_Limit_Exceeded()
    {
        _rateLimiter.TryConsumeAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("RATE-LIMITED");
        await _repo.DidNotReceive().AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
    }

    // ── Invalid password ──────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_When_Password_Is_Too_Short()
    {
        SetupHappyPath();
        var cmd = ValidCommand with { Password = "Ab1!" }; // < 10 chars
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("INVALID-PASSWORD");
    }
}
