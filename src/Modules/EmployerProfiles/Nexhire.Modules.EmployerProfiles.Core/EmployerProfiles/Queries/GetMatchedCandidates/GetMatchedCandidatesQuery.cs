using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Shared.Core.Results;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetMatchedCandidates;

public record GetMatchedCandidatesQuery(Guid UserId) : IQuery<IReadOnlyList<MatchedCandidateDto>>;

public class GetMatchedCandidatesQueryHandler : IQueryHandler<GetMatchedCandidatesQuery, IReadOnlyList<MatchedCandidateDto>>
{
    private readonly IEmployerProfileRepository _employerRepository;
    private readonly IDashboardProjectionStore _projectionStore;

    public GetMatchedCandidatesQueryHandler(
        IEmployerProfileRepository employerRepository,
        IDashboardProjectionStore projectionStore)
    {
        _employerRepository = employerRepository;
        _projectionStore = projectionStore;
    }

    public async Task<Result<IReadOnlyList<MatchedCandidateDto>>> Handle(GetMatchedCandidatesQuery request, CancellationToken cancellationToken)
    {
        var profile = await _employerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<IReadOnlyList<MatchedCandidateDto>>(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var matches = await _projectionStore.GetMatchedCandidatesAsync(request.UserId, cancellationToken);
        
        IReadOnlyList<MatchedCandidateDto> dtos = matches
            .Select(m => new MatchedCandidateDto(m.Id, m.PostingId, m.CandidateUserId, m.MatchScore, m.GeneratedOnUtc))
            .ToList();

        return Result.Success(dtos);
    }
}
