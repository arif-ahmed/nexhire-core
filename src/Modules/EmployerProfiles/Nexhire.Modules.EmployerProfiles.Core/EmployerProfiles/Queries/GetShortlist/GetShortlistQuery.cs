using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetShortlist;

public record GetShortlistQuery(Guid UserId, Guid ShortlistId) : IQuery<ShortlistDetailDto>;

public class GetShortlistQueryHandler : IQueryHandler<GetShortlistQuery, ShortlistDetailDto>
{
    private readonly IEmployerProfileRepository _employerRepository;
    private readonly IShortlistRepository _shortlistRepository;

    public GetShortlistQueryHandler(
        IEmployerProfileRepository employerRepository,
        IShortlistRepository shortlistRepository)
    {
        _employerRepository = employerRepository;
        _shortlistRepository = shortlistRepository;
    }

    public async Task<Result<ShortlistDetailDto>> Handle(GetShortlistQuery request, CancellationToken cancellationToken)
    {
        var profile = await _employerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<ShortlistDetailDto>(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var shortlist = await _shortlistRepository.GetByIdAsync(request.ShortlistId, cancellationToken);
        if (shortlist == null || shortlist.EmployerProfileId != profile.Id || shortlist.IsDeleted)
        {
            return Result.Failure<ShortlistDetailDto>(new Error("Shortlist.NotFound", "Shortlist not found."));
        }

        var memberDtos = shortlist.Members
            .Select(m => new ShortlistMemberDto(m.Id, m.CandidateUserId, m.MatchScore, m.AddedOnUtc))
            .ToList();

        var dto = new ShortlistDetailDto(
            shortlist.Id,
            shortlist.Name,
            memberDtos,
            shortlist.CreatedOnUtc,
            shortlist.UpdatedOnUtc);

        return Result.Success(dto);
    }
}
