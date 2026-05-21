using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Queries.GetShortlists;

public record GetShortlistsQuery(Guid UserId) : IQuery<IReadOnlyList<ShortlistDto>>;

public class GetShortlistsQueryHandler : IQueryHandler<GetShortlistsQuery, IReadOnlyList<ShortlistDto>>
{
    private readonly IEmployerProfileRepository _employerRepository;
    private readonly IShortlistRepository _shortlistRepository;

    public GetShortlistsQueryHandler(
        IEmployerProfileRepository employerRepository,
        IShortlistRepository shortlistRepository)
    {
        _employerRepository = employerRepository;
        _shortlistRepository = shortlistRepository;
    }

    public async Task<Result<IReadOnlyList<ShortlistDto>>> Handle(GetShortlistsQuery request, CancellationToken cancellationToken)
    {
        var profile = await _employerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<IReadOnlyList<ShortlistDto>>(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var shortlists = await _shortlistRepository.GetByEmployerProfileIdAsync(profile.Id, cancellationToken);
        
        IReadOnlyList<ShortlistDto> dtos = shortlists
            .Where(s => !s.IsDeleted)
            .Select(s => new ShortlistDto(s.Id, s.Name, s.Members.Count, s.CreatedOnUtc, s.UpdatedOnUtc))
            .ToList();

        return Result.Success(dtos);
    }
}
