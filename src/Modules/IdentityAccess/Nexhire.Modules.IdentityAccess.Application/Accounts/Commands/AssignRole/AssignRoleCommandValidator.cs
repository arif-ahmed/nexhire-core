using FluentValidation;
using Nexhire.Modules.IdentityAccess.Domain.Domain;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AssignRole;

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.Role)
            .IsEnumName(typeof(UserRole), caseSensitive: false);

        RuleFor(x => x.TargetUserId)
            .NotEmpty();
    }
}
