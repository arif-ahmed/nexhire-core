using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Queries.GetProfileCompleteness;

public record GetProfileCompletenessQuery(Guid UserId) : IQuery<CompletenessScoreDto>;

public class GetProfileCompletenessQueryHandler : IQueryHandler<GetProfileCompletenessQuery, CompletenessScoreDto>
{
    private readonly IJobSeekerProfileRepository _repository;

    public GetProfileCompletenessQueryHandler(IJobSeekerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CompletenessScoreDto>> Handle(GetProfileCompletenessQuery request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<CompletenessScoreDto>(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        return Result.Success(profile.Completeness.ToDto());
    }
}
