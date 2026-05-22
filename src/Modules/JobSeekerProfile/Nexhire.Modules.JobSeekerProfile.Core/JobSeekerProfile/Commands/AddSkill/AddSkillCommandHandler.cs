using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;
using Nexhire.Shared.Core.CQRS;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.AddSkill;

public class AddSkillCommandHandler : ICommandHandler<AddSkillCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly ITaxonomyApi _taxonomyApi;
    private readonly IUnitOfWork _unitOfWork;

    public AddSkillCommandHandler(
        IJobSeekerProfileRepository repository,
        ITaxonomyApi taxonomyApi,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _taxonomyApi = taxonomyApi;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddSkillCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var mapResult = await _taxonomyApi.MapSkillAsync(request.RawLabel, cancellationToken);
        if (mapResult.IsFailure)
        {
            return Result.Failure(mapResult.Error);
        }

        if (!Enum.TryParse<SkillCategory>(request.Category, true, out var category))
        {
            return Result.Failure(new Error("ProfileSkill.InvalidCategory", "Invalid skill category."));
        }

        if (!Enum.TryParse<SkillTier>(request.Tier, true, out var tier))
        {
            return Result.Failure(new Error("ProfileSkill.InvalidTier", "Invalid skill tier."));
        }

        var addResult = profile.AddSkill(
            mapResult.Value,
            request.RawLabel,
            category,
            tier,
            request.Proficiency);

        if (addResult.IsFailure)
        {
            return addResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
