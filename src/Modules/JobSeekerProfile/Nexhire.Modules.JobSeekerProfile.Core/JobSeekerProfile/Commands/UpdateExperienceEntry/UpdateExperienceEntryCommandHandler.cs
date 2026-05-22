using System.Text.Json;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UpdateExperienceEntry;

public class UpdateExperienceEntryCommandHandler : ICommandHandler<UpdateExperienceEntryCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IProfileHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateExperienceEntryCommandHandler(
        IJobSeekerProfileRepository repository,
        IProfileHistoryRepository historyRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateExperienceEntryCommand request, CancellationToken cancellationToken)
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

        var dateRangeResult = DateRange.Create(request.StartDate, request.EndDate);
        if (dateRangeResult.IsFailure)
        {
            return Result.Failure(dateRangeResult.Error);
        }

        var updateResult = profile.UpdateExperience(
            request.ExperienceEntryId,
            request.Company,
            request.Role,
            dateRangeResult.Value,
            request.IsCurrent,
            request.Responsibilities);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        var snapshotJson = JsonSerializer.Serialize(profile);
        var historyResult = history.AppendEdit(snapshotJson, new[] { "Experience" });
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
