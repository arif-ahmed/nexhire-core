using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Shared.Core.Results;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RemoveSkill;

public class RemoveSkillCommandHandler : ICommandHandler<RemoveSkillCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveSkillCommandHandler(
        IJobSeekerProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveSkillCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var removeResult = profile.RemoveSkill(request.SkillId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
