using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RejectEmployerVerification;

public class RejectEmployerVerificationCommandHandler : ICommandHandler<RejectEmployerVerificationCommand>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectEmployerVerificationCommandHandler(
        IEmployerProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RejectEmployerVerificationCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.ProfileId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var rejectionResult = profile.RejectManualVerification(request.AdminId, request.Reason);
        if (rejectionResult.IsFailure)
        {
            return rejectionResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
