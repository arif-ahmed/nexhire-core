using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetMyProfile;

public record GetMyProfileQuery(Guid UserId) : IQuery<JobSeekerProfileDto>;

public class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, JobSeekerProfileDto>
{
    private readonly IJobSeekerProfileRepository _repository;

    public GetMyProfileQueryHandler(IJobSeekerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<JobSeekerProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<JobSeekerProfileDto>(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        return Result.Success(profile.ToDto());
    }
}
