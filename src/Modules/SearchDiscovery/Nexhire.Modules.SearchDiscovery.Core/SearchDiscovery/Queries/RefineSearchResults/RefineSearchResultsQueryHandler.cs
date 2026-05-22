using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.RefineSearchResults;

public class RefineSearchResultsQueryHandler : IQueryHandler<RefineSearchResultsQuery, SearchResultDto>
{
    private readonly ISearchSessionRepository _sessionRepo;

    public RefineSearchResultsQueryHandler(ISearchSessionRepository sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public async Task<Result<SearchResultDto>> Handle(RefineSearchResultsQuery request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepo.GetBySeekerAsync(request.SeekerUserId, cancellationToken);
        if (session?.LastCriteria is null)
            return Result.Failure<SearchResultDto>(new Error("SearchSession.NoPreviousSearch", "No previous search to refine."));

        return Result.Success(new SearchResultDto([], [], 1, 20, 0, "Relevance", true));
    }
}
