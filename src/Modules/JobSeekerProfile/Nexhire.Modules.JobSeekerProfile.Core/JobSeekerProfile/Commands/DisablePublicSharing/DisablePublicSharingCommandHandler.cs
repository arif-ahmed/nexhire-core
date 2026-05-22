using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.DisablePublicSharing;

public class DisablePublicSharingCommandHandler : ICommandHandler<DisablePublicSharingCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DisablePublicSharingCommandHandler(
        IJobSeekerProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DisablePublicSharingCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var disableResult = profile.DisablePublicSharing();
        if (disableResult.IsFailure)
        {
            return disableResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
