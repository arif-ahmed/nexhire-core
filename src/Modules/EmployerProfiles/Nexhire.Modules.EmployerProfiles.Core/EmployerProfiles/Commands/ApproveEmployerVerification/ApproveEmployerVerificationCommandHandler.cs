using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.ApproveEmployerVerification;

public class ApproveEmployerVerificationCommandHandler : ICommandHandler<ApproveEmployerVerificationCommand>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveEmployerVerificationCommandHandler(
        IEmployerProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ApproveEmployerVerificationCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.ProfileId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var approvalResult = profile.ApproveManualVerification(request.AdminId, request.EvidenceRef);
        if (approvalResult.IsFailure)
        {
            return approvalResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
