using Nexhire.Modules.IdentityAccess.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUserAccountRepository _userRepository;

    public CreateUserCommandHandler(IUserAccountRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        var fullNameResult = FullName.Create(request.FirstName, request.LastName);
        if (fullNameResult.IsFailure)
        {
            return Result.Failure<Guid>(fullNameResult.Error);
        }

        var user = UserAccount.Create(Guid.NewGuid(), emailResult.Value, fullNameResult.Value);

        await _userRepository.AddAsync(user, cancellationToken);

        return Result.Success(user.Id);
    }
}
