using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.CreateSavedSearch;

public class CreateSavedSearchCommandHandler : ICommandHandler<CreateSavedSearchCommand, Guid>
{
    private readonly ISavedSearchRepository _savedSearchRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSavedSearchCommandHandler(ISavedSearchRepository savedSearchRepo, IUnitOfWork unitOfWork)
    {
        _savedSearchRepo = savedSearchRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateSavedSearchCommand request, CancellationToken cancellationToken)
    {
        var nameTaken = await _savedSearchRepo.IsNameTakenAsync(request.SeekerUserId, request.Name, null, cancellationToken);
        if (nameTaken)
            return Result.Failure<Guid>(new Error("E-SAVED-SEARCH-NAME-DUPLICATE", "A saved search with this name already exists."));

        var criteriaResult = SearchCriteria.Create(
            keyword: request.Keyword,
            allowEmptyForPersistence: true);

        if (criteriaResult.IsFailure)
            return Result.Failure<Guid>(criteriaResult.Error);

        var savedSearchResult = SavedSearch.Create(
            request.SeekerUserId,
            request.Name,
            criteriaResult.Value,
            request.NotificationPreference,
            DateTime.UtcNow);

        if (savedSearchResult.IsFailure)
            return Result.Failure<Guid>(savedSearchResult.Error);

        await _savedSearchRepo.AddAsync(savedSearchResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(savedSearchResult.Value.Id);
    }
}
