using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.CreateUser;

public record CreateUserCommand(string Email, string FirstName, string LastName) : ICommand<Guid>;
