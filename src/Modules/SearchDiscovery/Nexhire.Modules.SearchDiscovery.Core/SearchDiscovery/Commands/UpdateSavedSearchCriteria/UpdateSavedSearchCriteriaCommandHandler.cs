using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.UpdateSavedSearchCriteria;

public class UpdateSavedSearchCriteriaCommandHandler : ICommandHandler<UpdateSavedSearchCriteriaCommand>
{
    private readonly ISavedSearchRepository _savedSearchRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSavedSearchCriteriaCommandHandler(ISavedSearchRepository savedSearchRepo, IUnitOfWork unitOfWork)
    {
        _savedSearchRepo = savedSearchRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateSavedSearchCriteriaCommand request, CancellationToken cancellationToken)
    {
        var savedSearch = await _savedSearchRepo.GetByIdAsync(request.SavedSearchId, cancellationToken);
        if (savedSearch is null)
            return Result.Failure(new Error("E-SAVED-SEARCH-NOT-FOUND", "Saved search not found."));

        if (savedSearch.SeekerUserId != request.SeekerUserId)
            return Result.Failure(new Error("E-FORBIDDEN", "You do not own this saved search."));

        var criteriaResult = SearchCriteria.Create(keyword: request.Keyword, allowEmptyForPersistence: true);
        if (criteriaResult.IsFailure)
            return criteriaResult;

        var result = savedSearch.UpdateCriteria(criteriaResult.Value);
        if (result.IsFailure)
            return result;

        await _savedSearchRepo.UpdateAsync(savedSearch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
