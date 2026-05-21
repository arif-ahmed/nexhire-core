using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.Users.Core.Users.Commands.CreateUser;

public record CreateUserCommand(string Email, string FirstName, string LastName) : ICommand<Guid>;
