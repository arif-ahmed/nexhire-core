using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.SetSavedSearchNotification;

public class SetSavedSearchNotificationCommandHandler : ICommandHandler<SetSavedSearchNotificationCommand>
{
    private readonly ISavedSearchRepository _savedSearchRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SetSavedSearchNotificationCommandHandler(ISavedSearchRepository savedSearchRepo, IUnitOfWork unitOfWork)
    {
        _savedSearchRepo = savedSearchRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetSavedSearchNotificationCommand request, CancellationToken cancellationToken)
    {
        var savedSearch = await _savedSearchRepo.GetByIdAsync(request.SavedSearchId, cancellationToken);
        if (savedSearch is null)
            return Result.Failure(new Error("E-SAVED-SEARCH-NOT-FOUND", "Saved search not found."));

        if (savedSearch.SeekerUserId != request.SeekerUserId)
            return Result.Failure(new Error("E-FORBIDDEN", "You do not own this saved search."));

        var result = savedSearch.SetNotificationPreference(request.Preference);
        if (result.IsFailure)
            return result;

        await _savedSearchRepo.UpdateAsync(savedSearch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
