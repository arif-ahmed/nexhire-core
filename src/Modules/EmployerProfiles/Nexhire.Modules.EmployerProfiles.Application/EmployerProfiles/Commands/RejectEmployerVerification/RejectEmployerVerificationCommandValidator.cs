using FluentValidation;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.RejectEmployerVerification;

public class RejectEmployerVerificationCommandValidator : AbstractValidator<RejectEmployerVerificationCommand>
{
    public RejectEmployerVerificationCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Rejection reason is required.")
            .WithErrorCode("E-VERIFY-REJECTION-REASON-REQUIRED");
    }
}
