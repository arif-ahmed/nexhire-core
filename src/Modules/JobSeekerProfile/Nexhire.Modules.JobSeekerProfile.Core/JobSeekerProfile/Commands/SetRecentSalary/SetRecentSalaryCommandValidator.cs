using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetRecentSalary;

public class SetRecentSalaryCommandValidator : AbstractValidator<SetRecentSalaryCommand>
{
    public SetRecentSalaryCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Amount.HasValue)
            .WithMessage("Amount cannot be negative.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .When(x => x.Amount.HasValue)
            .WithMessage("Currency is required when amount is provided.");
    }
}
