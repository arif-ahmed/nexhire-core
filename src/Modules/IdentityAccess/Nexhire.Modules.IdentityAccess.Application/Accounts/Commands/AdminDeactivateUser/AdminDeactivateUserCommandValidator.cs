using FluentValidation;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminDeactivateUser;

public class AdminDeactivateUserCommandValidator : AbstractValidator<AdminDeactivateUserCommand>
{
    public AdminDeactivateUserCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty();
    }
}
