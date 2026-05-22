using FluentValidation;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.DeleteSavedSearch;

public class DeleteSavedSearchCommandValidator : AbstractValidator<DeleteSavedSearchCommand>
{
    public DeleteSavedSearchCommandValidator()
    {
        RuleFor(x => x.SavedSearchId).NotEmpty();
        RuleFor(x => x.SeekerUserId).NotEmpty();
    }
}
