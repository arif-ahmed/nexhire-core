using FluentAssertions;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ApiVersionRegistry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ExternalConnector;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.GovernmentAuditEntry;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.MappingProfile;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.Partner;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.SyncRecord;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.VerificationRequest;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Events;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Services;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Xunit;

namespace Nexhire.Modules.ExternalJobSync.Tests.Unit;

public class DomainTests
{
    [Fact]
    public void EmailAddress_Create_Should_LowerAndTrimValue_WhenValid()
    {
        // Act
        var result = EmailAddress.Create("  Test@NexHire.CoM  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("test@nexhire.com");
    }

    [Fact]
    public void EmailAddress_Create_Should_ReturnFailure_WhenMissingAtSign()
    {
        // Act
        var result = EmailAddress.Create("invalid-email");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Invalid");
    }

    [Fact]
    public void RateLimit_Create_Should_ReturnFailure_WhenNegativeRequests()
    {
        // Act
        var result = RateLimit.Create(-5, RateWindow.PerHour);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RateLimit.Invalid");
    }

    [Fact]
    public void EncryptedCredentials_ToString_Should_ReturnAsterisks_Always()
    {
        // Arrange
        var creds = EncryptedCredentials.Create("secret-cipher", "key-ref-1").Value;

        // Act
        var display = creds.ToString();

        // Assert
        display.Should().Be("***");
        creds.CipherText.Should().Be("secret-cipher");
    }

    [Fact]
    public void MinimisedRequestPayload_Create_Should_ReturnFailure_WhenFieldsNotWhitelisted()
    {
        // Arrange
        var fields = new Dictionary<string, string>
        {
            { "id_number", "12345" },
            { "super_secret_field", "value" }
        };

        // Act
        var result = MinimisedRequestPayload.Create(VerificationKind.Identity, fields);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Payload.NotWhitelisted");
    }

    [Fact]
    public void Partner_Register_Should_StartInPendingActivation_AndRaiseDomainEvent()
    {
        // Arrange
        var email = EmailAddress.Create("admin@sample.com").Value;

        // Act
        var result = Partner.Register("Job Portal X", email, "https://sample.com", "Info text");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var partner = result.Value;
        partner.Status.Should().Be(PartnerStatus.PendingActivation);
        partner.DomainEvents.Should().ContainSingle(e => e is PartnerRegistered);
    }

    [Fact]
    public void Partner_Approve_Should_TransitionToActive()
    {
        // Arrange
        var email = EmailAddress.Create("admin@sample.com").Value;
        var partner = Partner.Register("Job Portal X", email, "https://sample.com", "Info text").Value;

        // Act
        var result = partner.Approve();

        // Assert
        result.IsSuccess.Should().BeTrue();
        partner.Status.Should().Be(PartnerStatus.Active);
        partner.ActivatedOnUtc.Should().NotBeNull();
        partner.DomainEvents.Should().Contain(e => e is PartnerActivated);
    }

    [Fact]
    public void Partner_IssueApiKey_Should_EnforceAtMostOneActiveKey()
    {
        // Arrange
        var email = EmailAddress.Create("admin@sample.com").Value;
        var partner = Partner.Register("Job Portal X", email, "https://sample.com", "Info text").Value;
        partner.Approve();

        // Act
        partner.IssueApiKey(Guid.NewGuid(), "hash-1", "prefix-1");
        partner.IssueApiKey(Guid.NewGuid(), "hash-2", "prefix-2");

        // Assert
        partner.ApiKeys.Count(k => k.Status == ApiKeyStatus.Active).Should().Be(1);
        partner.ApiKeys.Count(k => k.Status == ApiKeyStatus.Revoked).Should().Be(1);
    }

    [Fact]
    public void SyncRecord_Quarantine_Should_OnlyBeReachableFromAccepted()
    {
        // Arrange
        var extRef = ExternalRef.Create("Portal", "job-1").Value;
        var record = SyncRecord.StartInbound(extRef, "{}", partnerId: Guid.NewGuid()).Value;
        
        // Act
        var result = record.Quarantine("E-MISSING-TITLE", "Title is missing");

        // Assert
        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(SyncStatus.Quarantined);
        record.ErrorCode.Should().Be("E-MISSING-TITLE");
        record.DomainEvents.Should().Contain(e => e is SyncErrorDetectedIntegrationEvent);
    }

    [Fact]
    public void SyncRecord_RecordAttempt_Should_TransitionToFailed_AfterThreeAttempts()
    {
        // Arrange
        var extRef = ExternalRef.Create("Portal", "job-1").Value;
        var record = SyncRecord.StartInbound(extRef, "{}", partnerId: Guid.NewGuid()).Value;

        // Act
        record.RecordAttempt(Guid.NewGuid(), AttemptOutcome.TransientFailure, 500, "Error 1", 100);
        record.RecordAttempt(Guid.NewGuid(), AttemptOutcome.TransientFailure, 500, "Error 2", 100);
        record.RecordAttempt(Guid.NewGuid(), AttemptOutcome.TransientFailure, 500, "Error 3", 100);

        // Assert
        record.Status.Should().Be(SyncStatus.Failed);
        record.ErrorCode.Should().Be("RETRY_LIMIT_EXCEEDED");
    }

    [Fact]
    public void VerificationRequest_StartIdentity_Should_ReturnFailure_WhenConsentIsFalse()
    {
        // Arrange
        var registry = Registry.Create("MoL", "http://mol.gov").Value;
        var consent = ConsentRecord.Create(false, "v1", DateTime.UtcNow).Value;
        var fields = new Dictionary<string, string> { { "id_number", "123" } };
        var payload = MinimisedRequestPayload.Create(VerificationKind.Identity, fields).Value;

        // Act
        var result = VerificationRequest.StartIdentity(Guid.NewGuid(), registry, consent, payload);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-GOV-CONSENT-REQUIRED");
    }

    [Fact]
    public void GovernmentAuditEntry_Record_Should_GenerateTamperEvidentHashChain()
    {
        // Arrange
        var queryParams = "{\"id_number\":\"12345\"}";
        var firstEntry = GovernmentAuditEntry.Record(
            Guid.NewGuid(), Guid.NewGuid(), "MoL", AuditDirection.Query, queryParams, "200", 25, "MaskId", "ConsentGranted", "").Value;

        // Act
        var secondEntry = GovernmentAuditEntry.Record(
            Guid.NewGuid(), Guid.NewGuid(), "MoL", AuditDirection.Query, queryParams, "200", 25, "MaskId", "ConsentGranted", firstEntry.IntegrityHash).Value;

        // Assert
        secondEntry.IntegrityHash.Should().NotBeEmpty();
        secondEntry.IntegrityHash.Should().NotBe(firstEntry.IntegrityHash);
    }

    [Fact]
    public void ApiVersionRegistry_DeprecateVersion_Should_EnforceSixMonthRule()
    {
        // Arrange
        var registry = ApiVersionRegistry.Create();
        registry.RegisterVersion("v1", DateTime.UtcNow.AddMonths(-12));

        // Act - Deprecating with sunset only 3 months out
        var result = registry.DeprecateVersion("v1", DateTime.UtcNow.AddMonths(3), "http://migration.guide");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ApiVersion.InvalidSunset");
    }

    [Fact]
    public void WebhookSignatureVerifier_Verify_Should_PassHMAC_WhenSignatureValid()
    {
        // Arrange
        var secret = EncryptedCredentials.Create("my-secret", "key-ref").Value;
        var body = "{\"event\":\"job_created\"}";
        
        // Compute signature manually
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes("my-secret"));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(body));
        var sigValue = Convert.ToHexString(hash).ToLowerInvariant();

        var sig = WebhookSignature.Create("HmacSha256", sigValue).Value;
        var verifier = new WebhookSignatureVerifier();

        // Act
        var result = verifier.Verify(body, sig, secret, WebhookSigningAlgorithm.HmacSha256);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
