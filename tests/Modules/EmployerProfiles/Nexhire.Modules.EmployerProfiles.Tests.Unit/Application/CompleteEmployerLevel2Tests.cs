using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.CompleteEmployerLevel2;
using Nexhire.Shared.Core.Results;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit.Application;

public class CompleteEmployerLevel2Tests
{
    private readonly IEmployerProfileRepository _repository = Substitute.For<IEmployerProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CompleteEmployerLevel2CommandHandler _handler;

    public CompleteEmployerLevel2Tests()
    {
        _handler = new CompleteEmployerLevel2CommandHandler(_repository, _unitOfWork);
    }

    private EmployerProfile CreateProfile(Guid userId)
    {
        return EmployerProfile.Register(
            Guid.NewGuid(),
            userId,
            CompanyName.Create("Nexhire Inc.").Value,
            EmailAddress.Create("info@nexhire.com").Value,
            MobileNumber.Create("+8801712345678").Value,
            CompanyIdentifier.Create("REG123456").Value);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenProfileNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var address = new AddressDto("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh");
        var command = new CompleteEmployerLevel2Command(
            userId,
            "https://nexhire.com",
            "Technology",
            "Medium",
            address,
            "Tech solutions");

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((EmployerProfile?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmployerProfile.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenProfileNotActivated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateProfile(userId); // in PendingActivation state

        var address = new AddressDto("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh");
        var command = new CompleteEmployerLevel2Command(
            userId,
            "https://nexhire.com",
            "Technology",
            "Medium",
            address,
            "Tech solutions");

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmployerProfile.NotActivated");
    }

    [Fact]
    public async Task Handle_ShouldSucceedAndCompleteL2_WhenProfileIsActivated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateProfile(userId);
        profile.Activate(); // moves to PendingVerification

        var address = new AddressDto("Line 1", null, "Dhaka", "Dhaka", "1212", "Bangladesh");
        var command = new CompleteEmployerLevel2Command(
            userId,
            "https://nexhire.com",
            "Technology",
            "Medium",
            address,
            "Tech solutions");

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Completeness.Level2Complete.Should().BeTrue();
        profile.Website!.Value.Should().Be(command.Website);
        profile.Industry.Should().Be(command.Industry);
        profile.CompanySize!.Value.Should().Be(CompanySizeEnum.Medium);
        profile.Address!.Line1.Should().Be(address.Line1);
        profile.Description!.Value.Should().Be(command.Description);

        await _repository.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_ShouldHaveErrors_WhenInputsAreInvalid()
    {
        // Arrange
        var validator = new CompleteEmployerLevel2CommandValidator();
        var command = new CompleteEmployerLevel2Command(
            Guid.Empty,
            "invalid-url",
            "",
            "InvalidSize",
            new AddressDto("", null, "", "", "", ""),
            new string('A', 5001));

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CompleteEmployerLevel2Command.UserId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CompleteEmployerLevel2Command.Website));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CompleteEmployerLevel2Command.Industry));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CompleteEmployerLevel2Command.CompanySize));
        result.Errors.Should().Contain(e => e.PropertyName == "Address.Line1");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CompleteEmployerLevel2Command.Description));
    }
}
