using FluentValidation;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.AddFavoriteJob;

public class AddFavoriteJobCommandValidator : AbstractValidator<AddFavoriteJobCommand>
{
    public AddFavoriteJobCommandValidator()
    {
        RuleFor(x => x.SeekerUserId).NotEmpty();
        RuleFor(x => x.PostingId).NotEmpty();
    }
}
