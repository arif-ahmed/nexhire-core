using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.GetSavedSearches;

public class GetSavedSearchesQueryHandler : IQueryHandler<GetSavedSearchesQuery, IReadOnlyList<SavedSearchDto>>
{
    private readonly ISavedSearchRepository _savedSearchRepo;

    public GetSavedSearchesQueryHandler(ISavedSearchRepository savedSearchRepo)
    {
        _savedSearchRepo = savedSearchRepo;
    }

    public async Task<Result<IReadOnlyList<SavedSearchDto>>> Handle(GetSavedSearchesQuery request, CancellationToken cancellationToken)
    {
        var searches = await _savedSearchRepo.ListBySeekerAsync(request.SeekerUserId, cancellationToken);

        var dtos = searches.Select(s => new SavedSearchDto(
            s.Id, s.Name,
            s.Criteria.Keyword,
            s.NotificationPreference.ToString(),
            s.LastEvaluatedOnUtc,
            s.CreatedOnUtc)).ToList();

        return Result.Success<IReadOnlyList<SavedSearchDto>>(dtos);
    }
}
