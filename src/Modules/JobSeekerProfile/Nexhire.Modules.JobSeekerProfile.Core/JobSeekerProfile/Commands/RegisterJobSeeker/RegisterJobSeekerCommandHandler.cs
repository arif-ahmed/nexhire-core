using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RegisterJobSeeker;

public class RegisterJobSeekerCommandHandler : ICommandHandler<RegisterJobSeekerCommand, Guid>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IProfileHistoryRepository _historyRepository;
    private readonly IIdentityProvisioningApi _identityApi;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterJobSeekerCommandHandler(
        IJobSeekerProfileRepository repository,
        IProfileHistoryRepository historyRepository,
        IIdentityProvisioningApi identityApi,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _historyRepository = historyRepository;
        _identityApi = identityApi;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RegisterJobSeekerCommand request, CancellationToken cancellationToken)
    {
        var provisionResult = await _identityApi.ProvisionCredentialAsync(
            request.Email,
            request.Mobile,
            request.Password,
            "JobSeeker",
            cancellationToken);

        if (provisionResult.IsFailure)
        {
            return Result.Failure<Guid>(provisionResult.Error);
        }

        if (!Enum.TryParse<Gender>(request.Gender, true, out var gender))
        {
            return Result.Failure<Guid>(new Error("E-REG-INVALID-GENDER", "Invalid gender value."));
        }

        var nameResult = PersonName.Create(request.FirstName, request.LastName);
        if (nameResult.IsFailure) return Result.Failure<Guid>(nameResult.Error);

        var emailResult = EmailAddress.Create(request.Email);
        if (emailResult.IsFailure) return Result.Failure<Guid>(emailResult.Error);

        var mobileResult = MobileNumber.Create(request.Mobile);
        if (mobileResult.IsFailure) return Result.Failure<Guid>(mobileResult.Error);

        var profileId = Guid.NewGuid();
        var profileResult = Aggregates.JobSeekerProfile.Register(
            profileId,
            provisionResult.Value.UserId,
            nameResult.Value,
            emailResult.Value,
            mobileResult.Value,
            gender);

        if (profileResult.IsFailure)
        {
            return Result.Failure<Guid>(profileResult.Error);
        }

        var historyResult = ProfileHistory.Start(Guid.NewGuid(), profileId);
        if (historyResult.IsFailure)
        {
            return Result.Failure<Guid>(historyResult.Error);
        }

        await _repository.AddAsync(profileResult.Value, cancellationToken);
        await _historyRepository.AddAsync(historyResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(profileId);
    }
}
