using FluentAssertions;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Services;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit;

public class DomainServicesTests
{
    [Theory]
    [InlineData(EmployerProfileStatus.PendingActivation, EmployerProfileStatus.PendingVerification, true)]
    [InlineData(EmployerProfileStatus.PendingActivation, EmployerProfileStatus.Verified, false)]
    [InlineData(EmployerProfileStatus.PendingVerification, EmployerProfileStatus.Verified, true)]
    [InlineData(EmployerProfileStatus.PendingVerification, EmployerProfileStatus.PendingManualVerification, true)]
    [InlineData(EmployerProfileStatus.PendingVerification, EmployerProfileStatus.Rejected, false)]
    [InlineData(EmployerProfileStatus.PendingManualVerification, EmployerProfileStatus.Verified, true)]
    [InlineData(EmployerProfileStatus.PendingManualVerification, EmployerProfileStatus.Rejected, true)]
    [InlineData(EmployerProfileStatus.Rejected, EmployerProfileStatus.PendingManualVerification, true)]
    [InlineData(EmployerProfileStatus.Rejected, EmployerProfileStatus.Verified, false)]
    [InlineData(EmployerProfileStatus.Verified, EmployerProfileStatus.Suspended, true)]
    [InlineData(EmployerProfileStatus.Verified, EmployerProfileStatus.Deactivated, true)]
    [InlineData(EmployerProfileStatus.Deactivated, EmployerProfileStatus.PendingActivation, false)]
    [InlineData(EmployerProfileStatus.Suspended, EmployerProfileStatus.Verified, true)]
    public void StatusTransition_Should_BeValidatedCorrectly(EmployerProfileStatus from, EmployerProfileStatus to, bool shouldSucceed)
    {
        // Act
        var result = VerificationStateMachine.EnsureTransitionAllowed(from, to);

        // Assert
        if (shouldSucceed)
        {
            result.IsSuccess.Should().BeTrue();
        }
        else
        {
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("StatusTransition.Invalid");
        }
    }

    [Theory]
    [InlineData(VerificationOutcome.NotStarted, VerificationOutcome.AutoPending, true)]
    [InlineData(VerificationOutcome.NotStarted, VerificationOutcome.ManualPassed, false)]
    [InlineData(VerificationOutcome.AutoPending, VerificationOutcome.AutoPassed, true)]
    [InlineData(VerificationOutcome.AutoPending, VerificationOutcome.AutoFailed, true)]
    [InlineData(VerificationOutcome.AutoFailed, VerificationOutcome.ManualPending, true)]
    [InlineData(VerificationOutcome.ManualPending, VerificationOutcome.ManualPassed, true)]
    [InlineData(VerificationOutcome.ManualPending, VerificationOutcome.ManualRejected, true)]
    [InlineData(VerificationOutcome.ManualRejected, VerificationOutcome.ManualPending, true)]
    public void VerificationOutcomeTransition_Should_BeValidatedCorrectly(VerificationOutcome from, VerificationOutcome to, bool shouldSucceed)
    {
        // Act
        var result = VerificationStateMachine.EnsureVerificationOutcomeAllowed(from, to);

        // Assert
        if (shouldSucceed)
        {
            result.IsSuccess.Should().BeTrue();
        }
        else
        {
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("VerificationOutcome.InvalidTransition");
        }
    }

    [Fact]
    public void ValidateLogo_Should_Succeed_WhenFileIsCleanImageAndUnderLimit()
    {
        // Arrange
        var file = FileReference.Create("key", "logo.png", "image/png", 2 * 1024 * 1024).Value;
        var scan = VirusScanResult.Create(VirusScanStatus.Clean).Value;

        // Act
        var result = UploadPolicyService.ValidateLogo(file, scan);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateLogo_Should_Fail_WhenFileIsInfected()
    {
        // Arrange
        var file = FileReference.Create("key", "logo.png", "image/png", 2 * 1024 * 1024).Value;
        var scan = VirusScanResult.Create(VirusScanStatus.Infected).Value;

        // Act
        var result = UploadPolicyService.ValidateLogo(file, scan);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-UPLOAD-VIRUS");
    }

    [Fact]
    public void ValidateLogo_Should_Fail_WhenFileIsOversized()
    {
        // Arrange
        var file = FileReference.Create("key", "logo.png", "image/png", 6 * 1024 * 1024).Value;
        var scan = VirusScanResult.Create(VirusScanStatus.Clean).Value;

        // Act
        var result = UploadPolicyService.ValidateLogo(file, scan);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-UPLOAD-SIZE-EXCEEDED");
    }

    [Fact]
    public void ValidateLogo_Should_Fail_WhenMimeIsUnsupported()
    {
        // Arrange
        var file = FileReference.Create("key", "doc.pdf", "application/pdf", 1024 * 1024).Value;
        var scan = VirusScanResult.Create(VirusScanStatus.Clean).Value;

        // Act
        var result = UploadPolicyService.ValidateLogo(file, scan);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-UPLOAD-INVALID-FORMAT");
    }

    [Fact]
    public void ValidateCompanyImage_Should_Fail_WhenGalleryIsFull()
    {
        // Arrange
        var file = FileReference.Create("key", "image.png", "image/png", 1024 * 1024).Value;
        var scan = VirusScanResult.Create(VirusScanStatus.Clean).Value;

        // Act
        var result = UploadPolicyService.ValidateCompanyImage(file, scan, 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-UPLOAD-LIMIT-EXCEEDED");
    }

    [Fact]
    public void ValidateSupplementaryDocument_Should_Succeed_ForPdfUnder10Mb()
    {
        // Arrange
        var file = FileReference.Create("key", "doc.pdf", "application/pdf", 8 * 1024 * 1024).Value;
        var scan = VirusScanResult.Create(VirusScanStatus.Clean).Value;

        // Act
        var result = UploadPolicyService.ValidateSupplementaryDocument(file, scan, 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateSupplementaryDocument_Should_Fail_WhenGalleryExceeds10Docs()
    {
        // Arrange
        var file = FileReference.Create("key", "doc.pdf", "application/pdf", 1024 * 1024).Value;
        var scan = VirusScanResult.Create(VirusScanStatus.Clean).Value;

        // Act
        var result = UploadPolicyService.ValidateSupplementaryDocument(file, scan, 10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-UPLOAD-LIMIT-EXCEEDED");
    }
}
