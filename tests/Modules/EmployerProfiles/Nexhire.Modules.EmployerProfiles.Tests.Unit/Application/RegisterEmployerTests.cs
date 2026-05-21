using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RegisterEmployer;
using Nexhire.Shared.Core.Results;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit.Application;

public class RegisterEmployerTests
{
    private readonly IEmployerProfileRepository _repository = Substitute.For<IEmployerProfileRepository>();
    private readonly IIdentityProvisioningApi _identityApi = Substitute.For<IIdentityProvisioningApi>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterEmployerCommandHandler _handler;

    public RegisterEmployerTests()
    {
        _handler = new RegisterEmployerCommandHandler(_repository, _identityApi, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCompanyIdentifierAlreadyExists()
    {
        // Arrange
        var command = new RegisterEmployerCommand(
            "info@nexhire.com",
            "+8801712345678",
            "StrongPass123!",
            "Nexhire Corp",
            "REG123456");

        _repository.CompanyIdentifierExistsAsync(command.CompanyIdentifier, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-REG-DUPLICATE");
        await _identityApi.DidNotReceiveWithAnyArgs().ProvisionCredentialAsync(default!, default!, default!, default!, default!);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default!);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenIdentityProvisioningFails()
    {
        // Arrange
        var command = new RegisterEmployerCommand(
            "info@nexhire.com",
            "+8801712345678",
            "StrongPass123!",
            "Nexhire Corp",
            "REG123456");

        _repository.CompanyIdentifierExistsAsync(command.CompanyIdentifier, Arg.Any<CancellationToken>())
            .Returns(false);

        var provisioningError = new Error("E-REG-PASSWORD-BREACHED", "Password is breached.");
        _identityApi.ProvisionCredentialAsync(command.Email, command.Mobile, command.Password, "Employer", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ProvisionedIdentity>(provisioningError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-REG-PASSWORD-BREACHED");
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default!);
    }

    [Fact]
    public async Task Handle_ShouldSucceedAndCreateProfile_WhenInputsAreValid()
    {
        // Arrange
        var command = new RegisterEmployerCommand(
            "info@nexhire.com",
            "+8801712345678",
            "StrongPass123!",
            "Nexhire Corp",
            "REG123456");

        _repository.CompanyIdentifierExistsAsync(command.CompanyIdentifier, Arg.Any<CancellationToken>())
            .Returns(false);

        var userId = Guid.NewGuid();
        _identityApi.ProvisionCredentialAsync(command.Email, command.Mobile, command.Password, "Employer", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ProvisionedIdentity(userId)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _repository.Received(1).AddAsync(Arg.Is<EmployerProfile>(p => 
            p.UserId == userId &&
            p.CompanyName.Value == command.CompanyName &&
            p.Email.Value == command.Email &&
            p.Mobile.Value == command.Mobile &&
            p.CompanyIdentifier.Value == command.CompanyIdentifier), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_ShouldHaveErrors_WhenInputsAreInvalid()
    {
        // Arrange
        var validator = new RegisterEmployerCommandValidator();
        var command = new RegisterEmployerCommand("", "", "", "", "");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterEmployerCommand.Email));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterEmployerCommand.Mobile));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterEmployerCommand.CompanyName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterEmployerCommand.CompanyIdentifier));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterEmployerCommand.Password));
    }
}
