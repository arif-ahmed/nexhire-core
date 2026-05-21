using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RequestEmployerVerification;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.ApproveEmployerVerification;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RejectEmployerVerification;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.ResubmitEmployerVerification;
using Nexhire.Shared.Core.Results;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit.Application;

public class EmployerVerificationTests
{
    private readonly IEmployerProfileRepository _repository = Substitute.For<IEmployerProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private EmployerProfile CreateLevel2CompleteProfile(Guid userId)
    {
        var profile = EmployerProfile.Register(
            Guid.NewGuid(),
            userId,
            CompanyName.Create("Nexhire Inc.").Value,
            EmailAddress.Create("info@nexhire.com").Value,
            MobileNumber.Create("+8801712345678").Value,
            CompanyIdentifier.Create("REG123456").Value);

        profile.Activate();

        var website = WebsiteUrl.Create("https://nexhire.com").Value;
        var address = Address.Create("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value;
        var desc = CompanyDescription.Create("Innovative tech solutions").Value;
        var companySize = CompanySize.Create(CompanySizeEnum.Medium).Value;

        profile.CompleteLevel2(website, "Technology", companySize, address, desc);
        return profile;
    }

    [Fact]
    public async Task RequestVerification_ShouldSucceed_WhenProfileIsL2Complete()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateLevel2CompleteProfile(userId);

        var handler = new RequestEmployerVerificationCommandHandler(_repository, _unitOfWork);
        var command = new RequestEmployerVerificationCommand(userId, "REG-REF-123");

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.PendingVerification);
        profile.Verification.Outcome.Should().Be(VerificationOutcome.AutoPending);

        await _repository.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveManualVerification_ShouldSucceed_WhenPendingManualVerification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateLevel2CompleteProfile(userId);
        profile.BeginAutomaticVerification("REG-REF-123");
        profile.RecordAutomaticVerificationFailed(); // moves to PendingManualVerification

        var handler = new ApproveEmployerVerificationCommandHandler(_repository, _unitOfWork);
        var command = new ApproveEmployerVerificationCommand(profile.Id, Guid.NewGuid(), "EVIDENCE-APPROVED");

        _repository.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.Verified);
        profile.Verification.Outcome.Should().Be(VerificationOutcome.ManualPassed);

        await _repository.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectManualVerification_ShouldSucceed_WhenReasonIsProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateLevel2CompleteProfile(userId);
        profile.BeginAutomaticVerification("REG-REF-123");
        profile.RecordAutomaticVerificationFailed();

        var handler = new RejectEmployerVerificationCommandHandler(_repository, _unitOfWork);
        var command = new RejectEmployerVerificationCommand(profile.Id, Guid.NewGuid(), "Failed audit papers.");

        _repository.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.Rejected);
        profile.Verification.Outcome.Should().Be(VerificationOutcome.ManualRejected);
        profile.Verification.RejectionReason.Should().Be("Failed audit papers.");

        await _repository.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResubmitVerification_ShouldFail_WhenNoChangesSinceLastAttempt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateLevel2CompleteProfile(userId);
        profile.BeginAutomaticVerification("REG-REF-123");
        profile.RecordAutomaticVerificationFailed();
        profile.RejectManualVerification(Guid.NewGuid(), "Fake papers");

        var handler = new ResubmitEmployerVerificationCommandHandler(_repository, _unitOfWork);
        var command = new ResubmitEmployerVerificationCommand(userId);

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-VERIFY-NO-CHANGES");
    }

    [Fact]
    public async Task ResubmitVerification_ShouldSucceed_WhenChangesExistSinceLastAttempt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateLevel2CompleteProfile(userId);
        profile.BeginAutomaticVerification("REG-REF-123");
        profile.RecordAutomaticVerificationFailed();
        profile.RejectManualVerification(Guid.NewGuid(), "Fake papers");

        // Act: modify profile info after rejection
        var industry = "Healthcare";
        profile.UpdateCompanyInformation(industry: industry);

        var handler = new ResubmitEmployerVerificationCommandHandler(_repository, _unitOfWork);
        var command = new ResubmitEmployerVerificationCommand(userId);

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(EmployerProfileStatus.PendingManualVerification);
        profile.Verification.Outcome.Should().Be(VerificationOutcome.ManualPending);

        await _repository.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
