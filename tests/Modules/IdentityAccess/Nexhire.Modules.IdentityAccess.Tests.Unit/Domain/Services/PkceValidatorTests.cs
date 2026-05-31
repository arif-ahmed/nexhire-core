using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.Services;

/// <summary>
/// Validates the PKCE S256 code_verifier / code_challenge round-trip (RFC 7636).
/// </summary>
public class PkceValidatorTests
{
    // A valid 43-character verifier per RFC 7636 §4.1
    private const string ValidVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

    public class ComputeS256Challenge
    {
        [Fact]
        public void Should_Produce_Base64Url_Encoded_String()
        {
            var challenge = PkceValidator.ComputeS256Challenge(ValidVerifier);

            // Base64Url must not contain +, /, or padding =
            challenge.Should().NotContain("+");
            challenge.Should().NotContain("/");
            challenge.Should().NotContain("=");
        }

        [Fact]
        public void Should_Produce_Deterministic_Output_For_Same_Input()
        {
            var c1 = PkceValidator.ComputeS256Challenge(ValidVerifier);
            var c2 = PkceValidator.ComputeS256Challenge(ValidVerifier);

            c1.Should().Be(c2);
        }

        [Fact]
        public void Should_Produce_Different_Challenges_For_Different_Verifiers()
        {
            var other = "anotherValidVerifier123456789012345678901234";

            var c1 = PkceValidator.ComputeS256Challenge(ValidVerifier);
            var c2 = PkceValidator.ComputeS256Challenge(other);

            c1.Should().NotBe(c2);
        }
    }

    public class Verify
    {
        // ── Happy path ────────────────────────────────────────────────────────

        [Fact]
        public void Should_Succeed_When_Verifier_Matches_Challenge()
        {
            var challenge = PkceValidator.ComputeS256Challenge(ValidVerifier);

            var result = PkceValidator.Verify(ValidVerifier, challenge);

            result.IsSuccess.Should().BeTrue();
        }

        // RFC 7636 §4.1 reference vector: verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
        // challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"
        [Fact]
        public void Should_Match_Rfc7636_Reference_Vector()
        {
            const string verifier  = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
            const string challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

            var result = PkceValidator.Verify(verifier, challenge);

            result.IsSuccess.Should().BeTrue(because: "RFC 7636 §4.6 reference vector must validate correctly");
        }

        // ── Wrong verifier ────────────────────────────────────────────────────

        [Fact]
        public void Should_Fail_When_Verifier_Does_Not_Match_Challenge()
        {
            var challenge = PkceValidator.ComputeS256Challenge(ValidVerifier);
            const string wrongVerifier = "WrongVerifier1234567890123456789012345678901";

            var result = PkceValidator.Verify(wrongVerifier, challenge);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OAUTH-INVALID-PKCE");
        }

        // ── Missing / empty verifier ──────────────────────────────────────────

        [Fact]
        public void Should_Fail_When_Verifier_Is_Empty()
        {
            var result = PkceValidator.Verify("", "some-challenge");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OAUTH-INVALID-PKCE");
        }

        [Fact]
        public void Should_Fail_When_Verifier_Is_Whitespace()
        {
            var result = PkceValidator.Verify("   ", "some-challenge");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OAUTH-INVALID-PKCE");
        }

        // ── Length constraints (RFC 7636 §4.1: 43–128 chars) ─────────────────

        [Fact]
        public void Should_Fail_When_Verifier_Is_Too_Short()
        {
            var shortVerifier = new string('a', 42); // one below minimum

            var result = PkceValidator.Verify(shortVerifier, "some-challenge");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OAUTH-INVALID-PKCE");
        }

        [Fact]
        public void Should_Fail_When_Verifier_Is_Too_Long()
        {
            var longVerifier = new string('a', 129); // one above maximum

            var result = PkceValidator.Verify(longVerifier, "some-challenge");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-OAUTH-INVALID-PKCE");
        }

        [Fact]
        public void Should_Accept_Minimum_Length_Verifier()
        {
            var minVerifier = new string('a', 43);
            var challenge = PkceValidator.ComputeS256Challenge(minVerifier);

            var result = PkceValidator.Verify(minVerifier, challenge);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Accept_Maximum_Length_Verifier()
        {
            var maxVerifier = new string('a', 128);
            var challenge = PkceValidator.ComputeS256Challenge(maxVerifier);

            var result = PkceValidator.Verify(maxVerifier, challenge);

            result.IsSuccess.Should().BeTrue();
        }
    }
}
