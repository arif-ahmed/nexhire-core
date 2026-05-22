using FluentValidation;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.CreateSavedSearch;

public class CreateSavedSearchCommandValidator : AbstractValidator<CreateSavedSearchCommand>
{
    public CreateSavedSearchCommandValidator()
    {
        RuleFor(x => x.SeekerUserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NotificationPreference).IsInEnum();
    }
}
