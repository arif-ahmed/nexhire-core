using FluentValidation;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminSuspendUser;

public class AdminSuspendUserCommandValidator : AbstractValidator<AdminSuspendUserCommand>
{
    public AdminSuspendUserCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty();
    }
}
