using FluentValidation;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RememberSearchCriteria;

public class RememberSearchCriteriaCommandValidator : AbstractValidator<RememberSearchCriteriaCommand>
{
    public RememberSearchCriteriaCommandValidator()
    {
        RuleFor(x => x.SeekerUserId).NotEmpty();
    }
}
