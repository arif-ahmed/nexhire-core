using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RemoveFavoriteJob;

public class RemoveFavoriteJobCommandHandler : ICommandHandler<RemoveFavoriteJobCommand>
{
    private readonly IFavoriteJobRepository _favoriteRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFavoriteJobCommandHandler(IFavoriteJobRepository favoriteRepo, IUnitOfWork unitOfWork)
    {
        _favoriteRepo = favoriteRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveFavoriteJobCommand request, CancellationToken cancellationToken)
    {
        var favorite = await _favoriteRepo.GetBySeekerAndPostingAsync(request.SeekerUserId, request.PostingId, cancellationToken);
        if (favorite is null)
            return Result.Success();

        favorite.Remove();
        await _favoriteRepo.DeleteAsync(favorite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
