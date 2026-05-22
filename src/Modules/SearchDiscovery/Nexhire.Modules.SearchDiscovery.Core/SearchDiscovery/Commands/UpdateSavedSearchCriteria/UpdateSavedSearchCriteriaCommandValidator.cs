using FluentValidation;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.UpdateSavedSearchCriteria;

public class UpdateSavedSearchCriteriaCommandValidator : AbstractValidator<UpdateSavedSearchCriteriaCommand>
{
    public UpdateSavedSearchCriteriaCommandValidator()
    {
        RuleFor(x => x.SavedSearchId).NotEmpty();
        RuleFor(x => x.SeekerUserId).NotEmpty();
    }
}
