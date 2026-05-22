using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Queries.GetSearchSession;

public class GetSearchSessionQueryHandler : IQueryHandler<GetSearchSessionQuery, SearchSessionDto>
{
    private readonly ISearchSessionRepository _sessionRepo;

    public GetSearchSessionQueryHandler(ISearchSessionRepository sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public async Task<Result<SearchSessionDto>> Handle(GetSearchSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepo.GetBySeekerAsync(request.SeekerUserId, cancellationToken);
        if (session is null)
            return Result.Failure<SearchSessionDto>(new Error("SearchSession.NotFound", "No active session found."));

        return Result.Success(new SearchSessionDto(
            session.Id,
            session.SeekerUserId,
            session.LastCriteria?.Keyword,
            session.DismissedRecommendationPostingIds.ToList(),
            session.ExpiresOnUtc));
    }
}
