using FluentValidation;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.SetSavedSearchNotification;

public class SetSavedSearchNotificationCommandValidator : AbstractValidator<SetSavedSearchNotificationCommand>
{
    public SetSavedSearchNotificationCommandValidator()
    {
        RuleFor(x => x.SavedSearchId).NotEmpty();
        RuleFor(x => x.SeekerUserId).NotEmpty();
        RuleFor(x => x.Preference).IsInEnum();
    }
}
