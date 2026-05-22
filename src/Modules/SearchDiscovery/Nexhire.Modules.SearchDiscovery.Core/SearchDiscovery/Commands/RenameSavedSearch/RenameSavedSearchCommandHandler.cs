using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RenameSavedSearch;

public class RenameSavedSearchCommandHandler : ICommandHandler<RenameSavedSearchCommand>
{
    private readonly ISavedSearchRepository _savedSearchRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RenameSavedSearchCommandHandler(ISavedSearchRepository savedSearchRepo, IUnitOfWork unitOfWork)
    {
        _savedSearchRepo = savedSearchRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RenameSavedSearchCommand request, CancellationToken cancellationToken)
    {
        var savedSearch = await _savedSearchRepo.GetByIdAsync(request.SavedSearchId, cancellationToken);
        if (savedSearch is null)
            return Result.Failure(new Error("E-SAVED-SEARCH-NOT-FOUND", "Saved search not found."));

        if (savedSearch.SeekerUserId != request.SeekerUserId)
            return Result.Failure(new Error("E-FORBIDDEN", "You do not own this saved search."));

        var nameTaken = await _savedSearchRepo.IsNameTakenAsync(request.SeekerUserId, request.NewName, request.SavedSearchId, cancellationToken);
        if (nameTaken)
            return Result.Failure(new Error("E-SAVED-SEARCH-NAME-DUPLICATE", "A saved search with this name already exists."));

        var result = savedSearch.Rename(request.NewName);
        if (result.IsFailure)
            return result;

        await _savedSearchRepo.UpdateAsync(savedSearch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
