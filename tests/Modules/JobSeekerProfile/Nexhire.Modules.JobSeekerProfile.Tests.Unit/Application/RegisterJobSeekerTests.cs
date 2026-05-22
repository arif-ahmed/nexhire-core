using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RegisterJobSeeker;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;
using Xunit;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Tests.Unit.Application;

public class RegisterJobSeekerTests
{
    private readonly IJobSeekerProfileRepository _repository = Substitute.For<IJobSeekerProfileRepository>();
    private readonly IProfileHistoryRepository _historyRepository = Substitute.For<IProfileHistoryRepository>();
    private readonly IIdentityProvisioningApi _identityApi = Substitute.For<IIdentityProvisioningApi>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterJobSeekerCommandHandler _handler;

    public RegisterJobSeekerTests()
    {
        _handler = new RegisterJobSeekerCommandHandler(_repository, _historyRepository, _identityApi, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenIdentityProvisioningFails()
    {
        // Arrange
        var command = new RegisterJobSeekerCommand(
            "john.doe@example.com",
            "+8801712345678",
            "StrongPass123!",
            "John",
            "Doe",
            "Male");

        var provisioningError = new Error("E-REG-PASSWORD-BREACHED", "Password is breached.");
        _identityApi.ProvisionCredentialAsync(command.Email, command.Mobile, command.Password, "JobSeeker", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ProvisionedIdentity>(provisioningError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-REG-PASSWORD-BREACHED");
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default!);
        await _historyRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default!);
    }

    [Fact]
    public async Task Handle_ShouldSucceedAndCreateProfileAndHistory_WhenInputsAreValid()
    {
        // Arrange
        var command = new RegisterJobSeekerCommand(
            "john.doe@example.com",
            "+8801712345678",
            "StrongPass123!",
            "John",
            "Doe",
            "Male");

        var userId = Guid.NewGuid();
        _identityApi.ProvisionCredentialAsync(command.Email, command.Mobile, command.Password, "JobSeeker", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ProvisionedIdentity(userId)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _repository.Received(1).AddAsync(Arg.Is<Aggregates.JobSeekerProfile>(p => 
            p.UserId == userId &&
            p.Name.First == command.FirstName &&
            p.Name.Last == command.LastName &&
            p.Email.Value == command.Email &&
            p.Mobile.Value == command.Mobile &&
            p.Gender == Gender.Male), Arg.Any<CancellationToken>());

        await _historyRepository.Received(1).AddAsync(Arg.Is<ProfileHistory>(h => 
            h.JobSeekerProfileId == result.Value), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_ShouldHaveErrors_WhenInputsAreInvalid()
    {
        // Arrange
        var validator = new RegisterJobSeekerCommandValidator();
        var command = new RegisterJobSeekerCommand("", "", "", "", "", "InvalidGender");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterJobSeekerCommand.Email));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterJobSeekerCommand.Mobile));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterJobSeekerCommand.FirstName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterJobSeekerCommand.LastName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterJobSeekerCommand.Password));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterJobSeekerCommand.Gender));
    }
}
