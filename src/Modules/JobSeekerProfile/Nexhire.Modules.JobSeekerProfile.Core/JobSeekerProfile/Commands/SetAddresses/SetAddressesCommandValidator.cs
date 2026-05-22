using FluentValidation;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetAddresses;

public class SetAddressesCommandValidator : AbstractValidator<SetAddressesCommand>
{
    public SetAddressesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.CurrentLine1)
            .NotEmpty().WithMessage("Current Line 1 is required.");

        RuleFor(x => x.CurrentCity)
            .NotEmpty().WithMessage("Current City is required.");

        RuleFor(x => x.CurrentDistrict)
            .NotEmpty().WithMessage("Current District is required.");

        RuleFor(x => x.CurrentPostcode)
            .NotEmpty().WithMessage("Current Postcode is required.");

        RuleFor(x => x.CurrentCountry)
            .NotEmpty().WithMessage("Current Country is required.");
    }
}
