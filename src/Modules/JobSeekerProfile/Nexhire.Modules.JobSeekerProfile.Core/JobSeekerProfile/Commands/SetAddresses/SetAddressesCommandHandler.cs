using System.Text.Json;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetAddresses;

public class SetAddressesCommandHandler : ICommandHandler<SetAddressesCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IProfileHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetAddressesCommandHandler(
        IJobSeekerProfileRepository repository,
        IProfileHistoryRepository historyRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetAddressesCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var history = await _historyRepository.GetByProfileIdAsync(profile.Id, cancellationToken);
        if (history == null)
        {
            return Result.Failure(new Error("ProfileHistory.NotFound", "Profile history not found."));
        }

        var currentAddressResult = Address.Create(
            request.CurrentLine1,
            request.CurrentLine2,
            request.CurrentCity,
            request.CurrentDistrict,
            request.CurrentPostcode,
            request.CurrentCountry);

        if (currentAddressResult.IsFailure)
        {
            return Result.Failure(currentAddressResult.Error);
        }

        Address? permanentAddress = null;
        if (!string.IsNullOrWhiteSpace(request.PermanentLine1) ||
            !string.IsNullOrWhiteSpace(request.PermanentCity) ||
            !string.IsNullOrWhiteSpace(request.PermanentDistrict) ||
            !string.IsNullOrWhiteSpace(request.PermanentPostcode) ||
            !string.IsNullOrWhiteSpace(request.PermanentCountry))
        {
            var permanentAddressResult = Address.Create(
                request.PermanentLine1 ?? string.Empty,
                request.PermanentLine2,
                request.PermanentCity ?? string.Empty,
                request.PermanentDistrict ?? string.Empty,
                request.PermanentPostcode ?? string.Empty,
                request.PermanentCountry ?? string.Empty);

            if (permanentAddressResult.IsFailure)
            {
                return Result.Failure(permanentAddressResult.Error);
            }

            permanentAddress = permanentAddressResult.Value;
        }

        var setAddressesResult = profile.SetAddresses(currentAddressResult.Value, permanentAddress);
        if (setAddressesResult.IsFailure)
        {
            return setAddressesResult;
        }

        var snapshotJson = JsonSerializer.Serialize(profile);
        var historyResult = history.AppendEdit(snapshotJson, new[] { "CurrentAddress", "PermanentAddress" });
        if (historyResult.IsFailure)
        {
            return historyResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _historyRepository.UpdateAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
