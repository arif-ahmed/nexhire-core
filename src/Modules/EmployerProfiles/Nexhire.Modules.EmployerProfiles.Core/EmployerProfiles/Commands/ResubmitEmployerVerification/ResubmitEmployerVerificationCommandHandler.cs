using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.ResubmitEmployerVerification;

public class ResubmitEmployerVerificationCommandHandler : ICommandHandler<ResubmitEmployerVerificationCommand>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ResubmitEmployerVerificationCommandHandler(
        IEmployerProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ResubmitEmployerVerificationCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        if (profile.Verification.LastAttemptUtc.HasValue && profile.UpdatedOnUtc <= profile.Verification.LastAttemptUtc.Value)
        {
            return Result.Failure(new Error("E-VERIFY-NO-CHANGES", "No changes have been made to the profile since the last verification attempt."));
        }

        var resubmitResult = profile.ResubmitForVerification();
        if (resubmitResult.IsFailure)
        {
            return resubmitResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
