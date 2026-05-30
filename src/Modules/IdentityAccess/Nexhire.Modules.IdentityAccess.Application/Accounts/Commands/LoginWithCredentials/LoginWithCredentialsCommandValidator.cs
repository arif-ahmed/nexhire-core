using FluentValidation;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.LoginWithCredentials;

public class LoginWithCredentialsCommandValidator : AbstractValidator<LoginWithCredentialsCommand>
{
    public LoginWithCredentialsCommandValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
