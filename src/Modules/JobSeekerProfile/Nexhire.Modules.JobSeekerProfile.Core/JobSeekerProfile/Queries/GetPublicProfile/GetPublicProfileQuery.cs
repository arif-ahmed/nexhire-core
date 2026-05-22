using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetPublicProfile;

public record GetPublicProfileQuery(string Slug) : IQuery<JobSeekerProfileDto>;

public class GetPublicProfileQueryHandler : IQueryHandler<GetPublicProfileQuery, JobSeekerProfileDto>
{
    private readonly IJobSeekerProfileRepository _repository;

    public GetPublicProfileQueryHandler(IJobSeekerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<JobSeekerProfileDto>> Handle(GetPublicProfileQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return Result.Failure<JobSeekerProfileDto>(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var profile = await _repository.GetBySlugAsync(request.Slug, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<JobSeekerProfileDto>(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        // PII Safety Check: Profile must be active and public sharing explicitly enabled.
        if (profile.Status != ProfileStatus.Active || !profile.PublicSharing.Enabled)
        {
            return Result.Failure<JobSeekerProfileDto>(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        return Result.Success(profile.ToDto());
    }
}
