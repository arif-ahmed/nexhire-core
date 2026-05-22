using FluentValidation;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RenameSavedSearch;

public class RenameSavedSearchCommandValidator : AbstractValidator<RenameSavedSearchCommand>
{
    public RenameSavedSearchCommandValidator()
    {
        RuleFor(x => x.SavedSearchId).NotEmpty();
        RuleFor(x => x.SeekerUserId).NotEmpty();
        RuleFor(x => x.NewName).NotEmpty().MaximumLength(100);
    }
}
