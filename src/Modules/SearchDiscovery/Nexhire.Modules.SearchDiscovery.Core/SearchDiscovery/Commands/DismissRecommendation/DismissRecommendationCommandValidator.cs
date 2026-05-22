using FluentValidation;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.DismissRecommendation;

public class DismissRecommendationCommandValidator : AbstractValidator<DismissRecommendationCommand>
{
    public DismissRecommendationCommandValidator()
    {
        RuleFor(x => x.SeekerUserId).NotEmpty();
        RuleFor(x => x.PostingId).NotEmpty();
    }
}
