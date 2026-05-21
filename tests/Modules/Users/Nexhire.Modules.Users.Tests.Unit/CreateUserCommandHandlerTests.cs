using FluentAssertions;
using Nexhire.Modules.Users.Core.Domain;
using Nexhire.Modules.Users.Core.Domain.Repositories;
using Nexhire.Modules.Users.Core.Users.Commands.CreateUser;
using NSubstitute;
using Xunit;

namespace Nexhire.Modules.Users.Tests.Unit;

public class CreateUserCommandHandlerTests
{
    private readonly IUserRepository _userRepositoryMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _userRepositoryMock = Substitute.For<IUserRepository>();
        _handler = new CreateUserCommandHandler(_userRepositoryMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessResult_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateUserCommand("test@nexhire.com", "John", "Doe");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _userRepositoryMock.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email.Value == "test@nexhire.com" && 
                             u.FullName.FirstName == "John" && 
                             u.FullName.LastName == "Doe"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailureResult_WhenEmailIsInvalid()
    {
        // Arrange
        var command = new CreateUserCommand("invalid-email", "John", "Doe");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Invalid");

        await _userRepositoryMock.DidNotReceive().AddAsync(
            Arg.Any<User>(),
            Arg.Any<CancellationToken>());
    }
}
