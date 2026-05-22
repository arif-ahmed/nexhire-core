using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetProfileVisibility;

public class SetProfileVisibilityCommandHandler : ICommandHandler<SetProfileVisibilityCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SetProfileVisibilityCommandHandler(
        IJobSeekerProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetProfileVisibilityCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        if (!Enum.TryParse<ProfileVisibility>(request.Visibility, true, out var visibility))
        {
            return Result.Failure(new Error("Profile.InvalidVisibility", "Invalid profile visibility."));
        }

        var result = profile.SetVisibility(visibility);
        if (result.IsFailure)
        {
            return result;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
