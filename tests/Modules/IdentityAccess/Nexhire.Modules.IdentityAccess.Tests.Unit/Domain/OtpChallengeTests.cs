using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.Domain;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain;

public class OtpChallengeTests
{
    private static readonly UserAccountId AccountId = new(Guid.NewGuid());
    private const string ValidCodeHash = "correct-code-hash";

    private static OtpChallenge IssueChallenge(
        OtpPurpose purpose = OtpPurpose.Activation,
        TimeSpan? ttl = null,
        int? maxAttempts = null)
    {
        return OtpChallenge.Issue(
            AccountId,
            purpose,
            ValidCodeHash,
            ttl ?? TimeSpan.FromMinutes(5),
            maxAttempts ?? (purpose == OtpPurpose.Activation ? 5 : 3));
    }

    // ── Issue ─────────────────────────────────────────────────────────────────

    public class Issue
    {
        [Fact]
        public void Should_Create_Challenge_With_Issued_Status()
        {
            var challenge = IssueChallenge();

            challenge.Status.Should().Be(OtpStatus.Issued);
        }

        [Fact]
        public void Should_Set_ExpiresOnUtc_Based_On_Ttl()
        {
            var before = DateTime.UtcNow;

            var challenge = IssueChallenge(ttl: TimeSpan.FromMinutes(5));

            challenge.ExpiresOnUtc.Should().BeAfter(before.AddMinutes(4));
            challenge.ExpiresOnUtc.Should().BeBefore(before.AddMinutes(6));
        }

        [Theory]
        [InlineData(OtpPurpose.Activation, 5)]
        [InlineData(OtpPurpose.Mfa, 3)]
        [InlineData(OtpPurpose.PasswordReset, 3)]
        public void Should_Set_Correct_MaxAttempts_Per_Purpose(OtpPurpose purpose, int expectedMax)
        {
            var challenge = IssueChallenge(purpose, maxAttempts: expectedMax);

            challenge.MaxAttempts.Should().Be(expectedMax);
        }

        [Fact]
        public void Should_Start_With_Zero_AttemptCount()
        {
            var challenge = IssueChallenge();

            challenge.AttemptCount.Should().Be(0);
        }
    }

    // ── Verify — happy path ───────────────────────────────────────────────────

    public class Verify
    {
        [Fact]
        public void Should_Succeed_With_Correct_Hash()
        {
            var challenge = IssueChallenge();
            var now = DateTime.UtcNow;

            var result = challenge.Verify(ValidCodeHash, now);

            result.IsSuccess.Should().BeTrue();
            challenge.Status.Should().Be(OtpStatus.Verified);
            challenge.VerifiedOnUtc.Should().Be(now);
        }

        [Fact]
        public void Should_Fail_With_Wrong_Hash_And_Increment_Attempt()
        {
            var challenge = IssueChallenge();

            var result = challenge.Verify("wrong-hash", DateTime.UtcNow);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OTP-INVALID");
            challenge.AttemptCount.Should().Be(1);
            challenge.Status.Should().Be(OtpStatus.Issued);
        }

        // ── Expiry ────────────────────────────────────────────────────────────

        [Fact]
        public void Should_Fail_With_OTP_EXPIRED_When_Past_ExpiresOnUtc()
        {
            var challenge = IssueChallenge(ttl: TimeSpan.FromSeconds(1));
            var pastExpiry = challenge.ExpiresOnUtc.AddSeconds(1);

            var result = challenge.Verify(ValidCodeHash, pastExpiry);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OTP-EXPIRED");
            challenge.Status.Should().Be(OtpStatus.Expired);
        }

        [Fact]
        public void Should_Fail_With_OTP_EXPIRED_When_Status_Already_Expired()
        {
            var challenge = IssueChallenge();
            challenge.MarkExpired();

            var result = challenge.Verify(ValidCodeHash, DateTime.UtcNow);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OTP-EXPIRED");
        }

        // ── Attempt limit → Locked ────────────────────────────────────────────

        [Fact]
        public void Should_Lock_After_Max_Failed_Attempts_For_Activation()
        {
            var challenge = IssueChallenge(OtpPurpose.Activation, maxAttempts: 5);
            var now = DateTime.UtcNow;

            for (var i = 0; i < 4; i++)
                challenge.Verify("wrong", now);

            // 5th attempt reaches the limit
            var result = challenge.Verify("wrong", now);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OTP-LOCKED");
            challenge.Status.Should().Be(OtpStatus.Locked);
        }

        [Fact]
        public void Should_Lock_After_3_Failed_Attempts_For_Mfa()
        {
            var challenge = IssueChallenge(OtpPurpose.Mfa, maxAttempts: 3);
            var now = DateTime.UtcNow;

            for (var i = 0; i < 2; i++)
                challenge.Verify("wrong", now);

            var result = challenge.Verify("wrong", now);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OTP-LOCKED");
            challenge.Status.Should().Be(OtpStatus.Locked);
        }

        [Fact]
        public void Should_Lock_After_3_Failed_Attempts_For_PasswordReset()
        {
            var challenge = IssueChallenge(OtpPurpose.PasswordReset, maxAttempts: 3);
            var now = DateTime.UtcNow;

            for (var i = 0; i < 2; i++)
                challenge.Verify("wrong", now);

            var result = challenge.Verify("wrong", now);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OTP-LOCKED");
        }

        // ── Terminal states — cannot re-verify ────────────────────────────────

        [Fact]
        public void Should_Fail_When_Already_Verified()
        {
            var challenge = IssueChallenge();
            challenge.Verify(ValidCodeHash, DateTime.UtcNow);

            var result = challenge.Verify(ValidCodeHash, DateTime.UtcNow);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OTP-ALREADY-VERIFIED");
        }

        [Fact]
        public void Should_Fail_When_Locked()
        {
            var challenge = IssueChallenge(OtpPurpose.Mfa, maxAttempts: 3);
            var now = DateTime.UtcNow;
            for (var i = 0; i < 3; i++)
                challenge.Verify("wrong", now);

            var result = challenge.Verify(ValidCodeHash, now);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OTP-LOCKED");
        }

        [Fact]
        public void AttemptCount_Should_Not_Exceed_MaxAttempts()
        {
            var challenge = IssueChallenge(OtpPurpose.Mfa, maxAttempts: 3);
            var now = DateTime.UtcNow;

            // Send 10 wrong attempts — should cap at MaxAttempts once locked
            for (var i = 0; i < 10; i++)
                challenge.Verify("wrong", now);

            challenge.AttemptCount.Should().BeLessOrEqualTo(challenge.MaxAttempts);
        }
    }

    // ── MarkExpired ───────────────────────────────────────────────────────────

    public class MarkExpired
    {
        [Fact]
        public void Should_Move_Issued_Challenge_To_Expired()
        {
            var challenge = IssueChallenge();

            challenge.MarkExpired();

            challenge.Status.Should().Be(OtpStatus.Expired);
        }

        [Fact]
        public void Should_Not_Affect_Already_Verified_Challenge()
        {
            var challenge = IssueChallenge();
            challenge.Verify(ValidCodeHash, DateTime.UtcNow);

            challenge.MarkExpired();

            challenge.Status.Should().Be(OtpStatus.Verified,
                because: "MarkExpired only transitions from Issued");
        }

        [Fact]
        public void Should_Not_Affect_Already_Locked_Challenge()
        {
            var challenge = IssueChallenge(OtpPurpose.Mfa, maxAttempts: 3);
            var now = DateTime.UtcNow;
            for (var i = 0; i < 3; i++)
                challenge.Verify("wrong", now);

            challenge.MarkExpired();

            challenge.Status.Should().Be(OtpStatus.Locked,
                because: "MarkExpired only transitions from Issued");
        }
    }
}
