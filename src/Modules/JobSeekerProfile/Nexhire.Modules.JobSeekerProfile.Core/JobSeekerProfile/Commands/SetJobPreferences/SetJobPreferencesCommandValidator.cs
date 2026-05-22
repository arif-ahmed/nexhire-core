using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetJobPreferences;

public class SetJobPreferencesCommandValidator : AbstractValidator<SetJobPreferencesCommand>
{
    public SetJobPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.JobTypes)
            .NotEmpty().WithMessage("At least one job type must be specified.");

        RuleFor(x => x.MinSalaryExpectation)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinSalaryExpectation.HasValue)
            .WithMessage("Minimum salary expectation cannot be negative.");

        RuleFor(x => x.MaxSalaryExpectation)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxSalaryExpectation.HasValue)
            .WithMessage("Maximum salary expectation cannot be negative.");

        RuleFor(x => x.MaxSalaryExpectation)
            .Must((cmd, max) => !cmd.MinSalaryExpectation.HasValue || !max.HasValue || cmd.MinSalaryExpectation.Value <= max.Value)
            .WithMessage("Minimum salary cannot exceed maximum salary.");
    }
}
