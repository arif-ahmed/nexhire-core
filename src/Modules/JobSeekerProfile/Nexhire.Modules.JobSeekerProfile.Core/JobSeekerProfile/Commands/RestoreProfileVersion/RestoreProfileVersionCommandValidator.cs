using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RestoreProfileVersion;

public class RestoreProfileVersionCommandValidator : AbstractValidator<RestoreProfileVersionCommand>
{
    public RestoreProfileVersionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.VersionId)
            .NotEmpty().WithMessage("VersionId is required.");
    }
}
