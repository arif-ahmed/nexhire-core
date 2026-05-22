using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetEditHistory;

public record GetEditHistoryQuery(Guid UserId) : IQuery<ProfileHistoryDto>;

public class GetEditHistoryQueryHandler : IQueryHandler<GetEditHistoryQuery, ProfileHistoryDto>
{
    private readonly IJobSeekerProfileRepository _profileRepository;
    private readonly IProfileHistoryRepository _historyRepository;

    public GetEditHistoryQueryHandler(
        IJobSeekerProfileRepository profileRepository,
        IProfileHistoryRepository historyRepository)
    {
        _profileRepository = profileRepository;
        _historyRepository = historyRepository;
    }

    public async Task<Result<ProfileHistoryDto>> Handle(GetEditHistoryQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<ProfileHistoryDto>(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var history = await _historyRepository.GetByProfileIdAsync(profile.Id, cancellationToken);
        if (history == null)
        {
            return Result.Failure<ProfileHistoryDto>(new Error("ProfileHistory.NotFound", "Profile history not found."));
        }

        return Result.Success(history.ToDto());
    }
}
