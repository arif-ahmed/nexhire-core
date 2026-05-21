using FluentValidation;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.CompleteEmployerLevel2;

public class CompleteEmployerLevel2CommandValidator : AbstractValidator<CompleteEmployerLevel2Command>
{
    public CompleteEmployerLevel2CommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Website)
            .NotEmpty().WithMessage("Website URL is required.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("A valid http or https absolute URL is required.");

        RuleFor(x => x.Industry)
            .NotEmpty().WithMessage("Industry is required.")
            .MaximumLength(100).WithMessage("Industry must not exceed 100 characters.");

        RuleFor(x => x.CompanySize)
            .NotEmpty().WithMessage("Company size is required.")
            .Must(size => System.Enum.TryParse<Domain.ValueObjects.CompanySizeEnum>(size, true, out _))
            .WithMessage("Company size must be Micro, Small, Medium, or Large.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters.");

        RuleFor(x => x.Address)
            .NotNull().WithMessage("Address is required.");

        RuleSet("AddressRules", () =>
        {
            RuleFor(x => x.Address.Line1)
                .NotEmpty().WithMessage("Address Line 1 is required.");
            RuleFor(x => x.Address.City)
                .NotEmpty().WithMessage("City is required.");
            RuleFor(x => x.Address.District)
                .NotEmpty().WithMessage("District is required.");
            RuleFor(x => x.Address.Country)
                .NotEmpty().WithMessage("Country is required.");
        });

        // Ensure flat rules check nested Address properties
        RuleFor(x => x.Address.Line1).NotEmpty().WithName("Address.Line1").WithMessage("Address Line 1 is required.");
        RuleFor(x => x.Address.City).NotEmpty().WithName("Address.City").WithMessage("City is required.");
        RuleFor(x => x.Address.District).NotEmpty().WithName("Address.District").WithMessage("District is required.");
        RuleFor(x => x.Address.Country).NotEmpty().WithName("Address.Country").WithMessage("Country is required.");
    }
}
