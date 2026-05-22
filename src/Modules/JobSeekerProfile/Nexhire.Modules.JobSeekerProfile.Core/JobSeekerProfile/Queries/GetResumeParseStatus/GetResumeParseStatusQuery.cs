using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetResumeParseStatus;

public record GetResumeParseStatusQuery(Guid UserId) : IQuery<ResumeParseStatusDto>;

public class GetResumeParseStatusQueryHandler : IQueryHandler<GetResumeParseStatusQuery, ResumeParseStatusDto>
{
    private readonly IJobSeekerProfileRepository _profileRepository;
    private readonly IResumeRepository _resumeRepository;

    public GetResumeParseStatusQueryHandler(
        IJobSeekerProfileRepository profileRepository,
        IResumeRepository resumeRepository)
    {
        _profileRepository = profileRepository;
        _resumeRepository = resumeRepository;
    }

    public async Task<Result<ResumeParseStatusDto>> Handle(GetResumeParseStatusQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<ResumeParseStatusDto>(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var resume = await _resumeRepository.GetActiveByProfileIdAsync(profile.Id, cancellationToken);
        if (resume == null)
        {
            return Result.Failure<ResumeParseStatusDto>(new Error("Resume.NotFound", "Active resume not found."));
        }

        return Result.Success(resume.ToDto());
    }
}
