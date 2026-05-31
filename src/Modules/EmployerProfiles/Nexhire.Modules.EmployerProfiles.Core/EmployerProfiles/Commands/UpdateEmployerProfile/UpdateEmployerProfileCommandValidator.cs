using FluentValidation;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UpdateEmployerProfile;

public class UpdateEmployerProfileCommandValidator : AbstractValidator<UpdateEmployerProfileCommand>
{
    public UpdateEmployerProfileCommandValidator()
    {
        When(x => x.CompanyName != null, () =>
        {
            RuleFor(x => x.CompanyName!)
                .NotEmpty().WithMessage("Company name cannot be empty.")
                .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.");
        });

        When(x => x.Website != null, () =>
        {
            RuleFor(x => x.Website!)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                .WithMessage("Website must be a valid http or https URL.")
                .WithErrorCode("E-PROFILE-INVALID-WEBSITE");
        });

        When(x => x.CompanySize != null, () =>
        {
            RuleFor(x => x.CompanySize!)
                .Must(s => Enum.TryParse<CompanySizeEnum>(s, ignoreCase: true, out _))
                .WithMessage("CompanySize must be one of: Micro, Small, Medium, Large.")
                .WithErrorCode("E-PROFILE-INVALID-COMPANY-SIZE");
        });

        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address!.Line1).NotEmpty().WithMessage("Address Line1 is required.");
            RuleFor(x => x.Address!.City).NotEmpty().WithMessage("Address City is required.");
            RuleFor(x => x.Address!.District).NotEmpty().WithMessage("Address District is required.");
            RuleFor(x => x.Address!.Country).NotEmpty().WithMessage("Address Country is required.");
        });

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description!)
                .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters.");
        });
    }
}
