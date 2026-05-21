using FluentAssertions;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Events;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;
using System.Reflection;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit;

public class AggregateTests
{
    private readonly Guid _profileId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly CompanyName _companyName = CompanyName.Create("Nexhire Inc.").Value;
    private readonly EmailAddress _email = EmailAddress.Create("info@nexhire.com").Value;
    private readonly MobileNumber _mobile = MobileNumber.Create("+8801712345678").Value;
    private readonly CompanyIdentifier _companyIdentifier = CompanyIdentifier.Create("REG123456").Value;
    private readonly CompanySize _companySize = CompanySize.Create(CompanySizeEnum.Medium).Value;

    private EmployerProfile CreateRegisteredProfile()
    {
        return EmployerProfile.Register(
            _profileId,
            _userId,
            _companyName,
            _email,
            _mobile,
            _companyIdentifier);
    }

    [Fact]
    public void Register_ShouldInitializeProfileInPendingActivationState()
    {
        // Act
        var profile = CreateRegisteredProfile();

        // Assert
        profile.Id.Should().Be(_profileId);
        profile.UserId.Should().Be(_userId);
        profile.Status.Should().Be(EmployerProfileStatus.PendingActivation);
        profile.CompanyName.Should().Be(_companyName);
        profile.Email.Should().Be(_email);
        profile.Mobile.Should().Be(_mobile);
        profile.CompanyIdentifier.Should().Be(_companyIdentifier);
        profile.IsVerified.Should().BeFalse();
        profile.Completeness.Level1Complete.Should().BeTrue();
        profile.Completeness.Level2Complete.Should().BeFalse();

        profile.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EmployerRegisteredIntegrationEvent>();
    }

    [Fact]
    public void Activate_ShouldTransitionToPendingVerification_WhenPendingActivation()
    {
        // Arrange
        var profile = CreateRegisteredProfile();

        // Act
        var result = profile.Activate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.PendingVerification);
        profile.DomainEvents.Should().Contain(e => e is EmployerProfileActivated);
    }

    [Fact]
    public void Activate_ShouldBeIdempotent_WhenAlreadyActivated()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();
        profile.ClearDomainEvents();

        // Act
        var result = profile.Activate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.PendingVerification);
        profile.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CompleteLevel2_ShouldFail_WhenPendingActivation()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var industry = "Technology";
        var size = _companySize;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;

        // Act
        var result = profile.CompleteLevel2(website, industry, size, address, desc);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmployerProfile.NotActivated");
    }

    [Fact]
    public void CompleteLevel2_ShouldSucceedAndRecomputeCompleteness_WhenActivated()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();

        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var industry = "Technology";
        var size = _companySize;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;

        // Act
        var result = profile.CompleteLevel2(website, industry, size, address, desc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Website.Should().Be(website);
        profile.Industry.Should().Be(industry);
        profile.CompanySize.Should().Be(size);
        profile.Address.Should().Be(address);
        profile.Description.Should().Be(desc);
        profile.Completeness.Level2Complete.Should().BeTrue();

        profile.DomainEvents.Should().Contain(e => e is EmployerProfileUpdatedIntegrationEvent);
    }

    [Fact]
    public void BeginAutomaticVerification_ShouldTransitionToAutoPending_WhenL2Complete()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();

        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;
        profile.CompleteLevel2(website, "Technology", _companySize, address, desc);

        // Act
        var result = profile.BeginAutomaticVerification("REG-REF-789");

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Verification.Outcome.Should().Be(VerificationOutcome.AutoPending);
        profile.Verification.Method.Should().Be(VerificationMethod.Automatic);
        profile.Status.Should().Be(EmployerProfileStatus.PendingVerification);

        profile.DomainEvents.Should().Contain(e => e is EmployerVerificationRequestedIntegrationEvent);
    }

    [Fact]
    public void BeginAutomaticVerification_ShouldFail_WhenL2Incomplete()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();

        // Act
        var result = profile.BeginAutomaticVerification("REG-REF-789");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmployerProfile.Level2Incomplete");
    }

    [Fact]
    public void RecordAutomaticVerificationPassed_ShouldTransitionToVerifiedWithBadge()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();
        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;
        profile.CompleteLevel2(website, "Technology", _companySize, address, desc);
        profile.BeginAutomaticVerification("REG-REF-789");

        // Act
        var result = profile.RecordAutomaticVerificationPassed("GOV-EVIDENCE-PASS");

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.Verified);
        profile.IsVerified.Should().BeTrue();
        profile.Verification.Outcome.Should().Be(VerificationOutcome.AutoPassed);
        profile.Verification.EvidenceRef.Should().Be("GOV-EVIDENCE-PASS");

        profile.DomainEvents.Should().Contain(e => e is EmployerVerifiedIntegrationEvent);
    }

    [Fact]
    public void RecordAutomaticVerificationFailed_ShouldTransitionToPendingManualVerification()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();
        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;
        profile.CompleteLevel2(website, "Technology", _companySize, address, desc);
        profile.BeginAutomaticVerification("REG-REF-789");

        // Act
        var result = profile.RecordAutomaticVerificationFailed();

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.PendingManualVerification);
        profile.Verification.Outcome.Should().Be(VerificationOutcome.ManualPending);

        profile.DomainEvents.Should().Contain(e => e is EmployerManualVerificationRequiredIntegrationEvent);
    }

    [Fact]
    public void ApproveManualVerification_ShouldSucceed_WhenPendingManualVerification()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();
        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;
        profile.CompleteLevel2(website, "Technology", _companySize, address, desc);
        profile.BeginAutomaticVerification("REG-REF-789");
        profile.RecordAutomaticVerificationFailed();

        var adminId = Guid.NewGuid();

        // Act
        var result = profile.ApproveManualVerification(adminId, "MANUAL-PASS-EVIDENCE");

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.Verified);
        profile.Verification.Outcome.Should().Be(VerificationOutcome.ManualPassed);
        profile.Verification.EvidenceRef.Should().Contain("MANUAL-PASS-EVIDENCE");

        profile.DomainEvents.Should().Contain(e => e is EmployerVerifiedIntegrationEvent);
    }

    [Fact]
    public void RejectManualVerification_ShouldSucceedAndRequireReason_WhenPendingManualVerification()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();
        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;
        profile.CompleteLevel2(website, "Technology", _companySize, address, desc);
        profile.BeginAutomaticVerification("REG-REF-789");
        profile.RecordAutomaticVerificationFailed();

        var adminId = Guid.NewGuid();

        // Act & Assert (empty reason fails)
        var failResult = profile.RejectManualVerification(adminId, "");
        failResult.IsFailure.Should().BeTrue();
        failResult.Error.Code.Should().Be("EmployerProfile.RejectionReasonRequired");

        // Act (valid reason succeeds)
        var successResult = profile.RejectManualVerification(adminId, "Fake registration papers.");
        successResult.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.Rejected);
        profile.Verification.Outcome.Should().Be(VerificationOutcome.ManualRejected);
        profile.Verification.RejectionReason.Should().Be("Fake registration papers.");

        profile.DomainEvents.Should().Contain(e => e is EmployerVerificationFailedIntegrationEvent);
    }

    [Fact]
    public void ResubmitForVerification_ShouldSucceed_WhenRejected()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();
        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;
        profile.CompleteLevel2(website, "Technology", _companySize, address, desc);
        profile.BeginAutomaticVerification("REG-REF-789");
        profile.RecordAutomaticVerificationFailed();
        profile.RejectManualVerification(Guid.NewGuid(), "Bad papers");

        // Act
        var result = profile.ResubmitForVerification();

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.PendingManualVerification);
        profile.Verification.Outcome.Should().Be(VerificationOutcome.ManualPending);

        profile.DomainEvents.Should().Contain(e => e is EmployerManualVerificationRequiredIntegrationEvent);
    }

    [Fact]
    public void SetLogo_ShouldEnforceUploadPolicies()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();

        var fileClean = FileReference.Create("logos/clean.png", "clean.png", "image/png", 1024).Value;
        var scanClean = VirusScanResult.Create(VirusScanStatus.Clean).Value;

        var fileInfected = FileReference.Create("logos/bad.png", "bad.png", "image/png", 1024).Value;
        var scanInfected = VirusScanResult.Create(VirusScanStatus.Infected).Value;

        var fileWrongMime = FileReference.Create("logos/gif.gif", "gif.gif", "image/gif", 1024).Value;

        // Act & Assert (Infected fails)
        var res1 = profile.SetLogo(fileInfected, scanInfected);
        res1.IsFailure.Should().BeTrue();
        res1.Error.Code.Should().Be("E-UPLOAD-VIRUS");

        // Act & Assert (Wrong format fails)
        var res2 = profile.SetLogo(fileWrongMime, scanClean);
        res2.IsFailure.Should().BeTrue();
        res2.Error.Code.Should().Be("E-UPLOAD-INVALID-FORMAT");

        // Act & Assert (Clean succeeds)
        var res3 = profile.SetLogo(fileClean, scanClean);
        res3.IsSuccess.Should().BeTrue();
        profile.Logo.Should().Be(fileClean);
    }

    [Fact]
    public void AddCompanyImage_ShouldEnforceMaxFiveLimit()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();

        var scan = VirusScanResult.Create(VirusScanStatus.Clean).Value;

        for (int i = 0; i < 5; i++)
        {
            var file = FileReference.Create($"images/{i}.png", $"{i}.png", "image/png", 1024).Value;
            var res = profile.AddCompanyImage(file, scan);
            res.IsSuccess.Should().BeTrue();
        }

        profile.Images.Should().HaveCount(5);

        // Act (6th image fails)
        var extraFile = FileReference.Create("images/6.png", "6.png", "image/png", 1024).Value;
        var extraRes = profile.AddCompanyImage(extraFile, scan);

        // Assert
        extraRes.IsFailure.Should().BeTrue();
        extraRes.Error.Code.Should().Be("E-UPLOAD-LIMIT-EXCEEDED");
    }

    [Fact]
    public void AddSupplementaryDocument_ShouldEnforceMaxTenLimit()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();

        var scan = VirusScanResult.Create(VirusScanStatus.Clean).Value;

        for (int i = 0; i < 10; i++)
        {
            var file = FileReference.Create($"docs/{i}.pdf", $"{i}.pdf", "application/pdf", 1024).Value;
            var res = profile.AddSupplementaryDocument(file, DocumentKind.RegistrationCertificate, scan);
            res.IsSuccess.Should().BeTrue();
        }

        profile.Documents.Should().HaveCount(10);

        // Act (11th document fails)
        var extraFile = FileReference.Create("docs/11.pdf", "11.pdf", "application/pdf", 1024).Value;
        var extraRes = profile.AddSupplementaryDocument(extraFile, DocumentKind.RegistrationCertificate, scan);

        // Assert
        extraRes.IsFailure.Should().BeTrue();
        extraRes.Error.Code.Should().Be("E-UPLOAD-LIMIT-EXCEEDED");
    }

    [Fact]
    public void Suspend_ShouldDeactivateProfileBehaviors()
    {
        // Arrange
        var profile = CreateRegisteredProfile();
        profile.Activate();

        // Act
        var suspendRes = profile.Suspend("Breach of terms");

        // Assert
        suspendRes.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.Suspended);

        // Act post-suspend: mutate L2 (should fail)
        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;
        var l2Res = profile.CompleteLevel2(website, "Tech", _companySize, address, desc);

        l2Res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Shortlist_ShouldNotAllowDuplicateCandidates()
    {
        // Arrange
        var shortlistResult = Shortlist.Create(Guid.NewGuid(), Guid.NewGuid(), "Awesome Devs");
        shortlistResult.IsSuccess.Should().BeTrue();
        var shortlist = shortlistResult.Value;

        var candidateId = Guid.NewGuid();

        // Act
        var add1 = shortlist.AddCandidate(candidateId, 95);
        var add2 = shortlist.AddCandidate(candidateId, 80);

        // Assert
        add1.IsSuccess.Should().BeTrue();
        add2.IsFailure.Should().BeTrue();
        add2.Error.Code.Should().Be("Shortlist.DuplicateCandidate");
        shortlist.Members.Should().ContainSingle();
    }

    [Fact]
    public void Shortlist_ShouldRejectMutations_WhenSoftDeleted()
    {
        // Arrange
        var shortlistResult = Shortlist.Create(Guid.NewGuid(), Guid.NewGuid(), "Awesome Devs");
        var shortlist = shortlistResult.Value;
        shortlist.Delete();

        // Act
        var renameRes = shortlist.Rename("New Name");
        var addRes = shortlist.AddCandidate(Guid.NewGuid());

        // Assert
        renameRes.IsFailure.Should().BeTrue();
        addRes.IsFailure.Should().BeTrue();
        shortlist.IsDeleted.Should().BeTrue();
    }
}
