using FluentValidation;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RemoveFavoriteJob;

public class RemoveFavoriteJobCommandValidator : AbstractValidator<RemoveFavoriteJobCommand>
{
    public RemoveFavoriteJobCommandValidator()
    {
        RuleFor(x => x.SeekerUserId).NotEmpty();
        RuleFor(x => x.PostingId).NotEmpty();
    }
}
