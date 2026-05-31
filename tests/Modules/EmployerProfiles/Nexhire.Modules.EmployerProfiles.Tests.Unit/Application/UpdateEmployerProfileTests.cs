using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Domain.ValueObjects;
using Nexhire.Modules.EmployerProfiles.Application.DTOs;
using Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.UpdateEmployerProfile;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit.Application;

public class UpdateEmployerProfileTests
{
    private readonly IEmployerProfileRepository _repository = Substitute.For<IEmployerProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateEmployerProfileCommandHandler _handler;
    private readonly UpdateEmployerProfileCommandValidator _validator = new();

    public UpdateEmployerProfileTests()
    {
        _handler = new UpdateEmployerProfileCommandHandler(_repository, _unitOfWork);
    }

    private static EmployerProfile CreateActivatedProfile(Guid userId)
    {
        var profile = EmployerProfile.Register(
            Guid.NewGuid(),
            userId,
            CompanyName.Create("Nexhire Inc.").Value,
            EmailAddress.Create("info@nexhire.com").Value,
            MobileNumber.Create("+8801712345678").Value,
            CompanyIdentifier.Create($"REG{Guid.NewGuid():N}").Value);
        profile.Activate();
        return profile;
    }

    [Fact]
    public async Task Handle_HappyPath_UpdatesFieldsAndReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var profile = CreateActivatedProfile(userId);
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var command = new UpdateEmployerProfileCommand(
            userId,
            "Updated Corp",
            "https://updated.com",
            "Finance",
            "Large",
            new AddressDto("123 Street", null, "Dhaka", "Dhaka", "1212", "Bangladesh"),
            "Updated description");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        profile.CompanyName.Value.Should().Be("Updated Corp");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenProfileDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((EmployerProfile?)null);

        var command = new UpdateEmployerProfileCommand(userId, "Name", null, null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmployerProfile.NotFound");
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenStatusIsSuspended()
    {
        var userId = Guid.NewGuid();
        var profile = CreateActivatedProfile(userId);
        profile.Suspend("Test suspension");
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var command = new UpdateEmployerProfileCommand(userId, "New Name", null, null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenStatusIsPendingActivation()
    {
        var userId = Guid.NewGuid();
        var profile = EmployerProfile.Register(
            Guid.NewGuid(), userId,
            CompanyName.Create("Corp").Value,
            EmailAddress.Create("e@e.com").Value,
            MobileNumber.Create("+8801711111111").Value,
            CompanyIdentifier.Create($"REG{Guid.NewGuid():N}").Value);
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var command = new UpdateEmployerProfileCommand(userId, "New Name", null, null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmployerProfile.NotActivated");
    }

    [Fact]
    public void Validator_ShouldFail_WhenWebsiteIsInvalid()
    {
        var command = new UpdateEmployerProfileCommand(Guid.NewGuid(), null, "not-a-url", null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Website);
    }

    [Fact]
    public void Validator_ShouldFail_WhenDescriptionExceeds5000Chars()
    {
        var command = new UpdateEmployerProfileCommand(Guid.NewGuid(), null, null, null, null, null, new string('x', 5001));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validator_ShouldFail_WhenCompanySizeIsUnknown()
    {
        var command = new UpdateEmployerProfileCommand(Guid.NewGuid(), null, null, null, "Giant", null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CompanySize);
    }
}
