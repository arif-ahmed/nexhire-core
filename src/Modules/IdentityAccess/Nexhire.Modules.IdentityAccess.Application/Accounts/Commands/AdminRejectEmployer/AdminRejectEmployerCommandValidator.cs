using FluentValidation;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminRejectEmployer;

public class AdminRejectEmployerCommandValidator : AbstractValidator<AdminRejectEmployerCommand>
{
    public AdminRejectEmployerCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty();
    }
}
