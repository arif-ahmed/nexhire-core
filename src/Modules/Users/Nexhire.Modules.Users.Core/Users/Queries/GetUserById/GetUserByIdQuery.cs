using Nexhire.Modules.Users.Core.DTOs;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.Users.Core.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>;
