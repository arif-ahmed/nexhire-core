using Nexhire.Modules.Users.Core.Domain;
using Nexhire.Modules.Users.Core.Domain.Repositories;
using Nexhire.Modules.Users.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Users.Core.Users.Commands.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        var fullNameResult = FullName.Create(request.FirstName, request.LastName);
        if (fullNameResult.IsFailure)
        {
            return Result.Failure<Guid>(fullNameResult.Error);
        }

        var user = User.Create(Guid.NewGuid(), emailResult.Value, fullNameResult.Value);

        await _userRepository.AddAsync(user, cancellationToken);

        return Result.Success(user.Id);
    }
}
