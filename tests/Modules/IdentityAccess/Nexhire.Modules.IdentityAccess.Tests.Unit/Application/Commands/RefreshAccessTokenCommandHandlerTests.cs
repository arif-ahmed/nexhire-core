using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.RefreshAccessToken;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Application.Commands;

public class RefreshAccessTokenCommandHandlerTests
{
    private readonly IUserAccountRepository _repo    = Substitute.For<IUserAccountRepository>();
    private readonly IJwtSigner             _jwt     = Substitute.For<IJwtSigner>();
    private readonly IRevokedTokenStore     _revoked = Substitute.For<IRevokedTokenStore>();

    private RefreshAccessTokenCommandHandler CreateHandler() =>
        new(_repo, _jwt, _revoked);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static UserAccount MakeActiveAccountWithSession(string refreshToken, out string tokenHash)
    {
        var perms = PermissionResolver.Resolve(UserRole.JobSeeker, []);
        var account = UserAccount.Provision(
            EmailAddress.Create("user@example.com").Value,
            MobileNumber.Create("+8801700000001").Value,
            PasswordHash.Create("$argon2id$h").Value,
            UserRole.JobSeeker, perms);
        account.Activate();
        tokenHash = Hash(refreshToken);
        var fp = DeviceFingerprint.Create("fp-1").Value;
        account.RecordSuccessfulLogin(Channel.Web, fp, tokenHash, DateTime.UtcNow.AddDays(30));
        account.ClearDomainEvents();
        return account;
    }

    // ── Happy path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Issue_New_Tokens_For_Valid_Refresh_Token()
    {
        const string rawToken = "valid-refresh-token";
        var account = MakeActiveAccountWithSession(rawToken, out var hash);

        _revoked.IsRevokedAsync(hash, Arg.Any<CancellationToken>()).Returns(false);
        _repo.GetBySessionRefreshTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(account);
        _jwt.IssueRefreshToken().Returns(("new-refresh", "new-hash"));
        _jwt.SignAccessToken(Arg.Any<AccessTokenSpec>()).Returns("new-access-jwt");

        var handler = CreateHandler();
        var result = await handler.Handle(new RefreshAccessTokenCommand(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-jwt");
        result.Value.RefreshToken.Should().Be("new-refresh");
    }

    [Fact]
    public async Task Should_Revoke_Old_Session_On_Refresh()
    {
        const string rawToken = "valid-refresh-token";
        var account = MakeActiveAccountWithSession(rawToken, out var hash);

        _revoked.IsRevokedAsync(hash, Arg.Any<CancellationToken>()).Returns(false);
        _repo.GetBySessionRefreshTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(account);
        _jwt.IssueRefreshToken().Returns(("new-refresh", "new-hash"));
        _jwt.SignAccessToken(Arg.Any<AccessTokenSpec>()).Returns("jwt");

        var handler = CreateHandler();
        await handler.Handle(new RefreshAccessTokenCommand(rawToken), CancellationToken.None);

        // The old session's refresh token is now revoked
        account.Sessions.First(s => s.RefreshTokenHash == hash).IsRevoked.Should().BeTrue(
            because: "refresh tokens are one-time-use (invariant #12)");
    }

    // ── Revoked token ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_E_TOKEN_REVOKED_For_Revoked_Token()
    {
        const string rawToken = "revoked-token";
        var hash = Hash(rawToken);

        _revoked.IsRevokedAsync(hash, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var result = await handler.Handle(new RefreshAccessTokenCommand(rawToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TOKEN-REVOKED");
    }

    // ── Unknown token ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_E_TOKEN_INVALID_For_Unknown_Token()
    {
        const string rawToken = "unknown-token";
        var hash = Hash(rawToken);

        _revoked.IsRevokedAsync(hash, Arg.Any<CancellationToken>()).Returns(false);
        _repo.GetBySessionRefreshTokenHashAsync(hash, Arg.Any<CancellationToken>())
            .Returns((UserAccount?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new RefreshAccessTokenCommand(rawToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-TOKEN-INVALID");
    }
}
