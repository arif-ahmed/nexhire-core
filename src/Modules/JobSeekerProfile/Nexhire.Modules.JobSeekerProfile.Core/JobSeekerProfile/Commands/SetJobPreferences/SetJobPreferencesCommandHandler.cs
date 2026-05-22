using System.Text.Json;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetJobPreferences;

public class SetJobPreferencesCommandHandler : ICommandHandler<SetJobPreferencesCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IProfileHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetJobPreferencesCommandHandler(
        IJobSeekerProfileRepository repository,
        IProfileHistoryRepository historyRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetJobPreferencesCommand request, CancellationToken cancellationToken)
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

        SalaryExpectation? salaryExpectation = null;
        if (request.MinSalaryExpectation.HasValue && request.MaxSalaryExpectation.HasValue)
        {
            var currency = request.SalaryCurrency ?? "BDT";
            var minMoneyResult = Money.Create(request.MinSalaryExpectation.Value, currency);
            if (minMoneyResult.IsFailure)
            {
                return Result.Failure(minMoneyResult.Error);
            }

            var maxMoneyResult = Money.Create(request.MaxSalaryExpectation.Value, currency);
            if (maxMoneyResult.IsFailure)
            {
                return Result.Failure(maxMoneyResult.Error);
            }

            var salaryExpectationResult = SalaryExpectation.Create(minMoneyResult.Value, maxMoneyResult.Value);
            if (salaryExpectationResult.IsFailure)
            {
                return Result.Failure(salaryExpectationResult.Error);
            }

            salaryExpectation = salaryExpectationResult.Value;
        }

        var arrangements = new List<WorkArrangement>();
        foreach (var arrStr in request.WorkArrangements ?? Enumerable.Empty<string>())
        {
            if (!Enum.TryParse<WorkArrangement>(arrStr, true, out var arr))
            {
                return Result.Failure(new Error("JobPreferences.InvalidWorkArrangement", $"Invalid work arrangement: {arrStr}"));
            }
            arrangements.Add(arr);
        }

        var preferencesResult = JobPreferences.Create(
            request.JobTypes,
            request.Industries,
            request.Locations,
            arrangements,
            salaryExpectation);

        if (preferencesResult.IsFailure)
        {
            return Result.Failure(preferencesResult.Error);
        }

        var setPreferencesResult = profile.SetPreferences(preferencesResult.Value);
        if (setPreferencesResult.IsFailure)
        {
            return setPreferencesResult;
        }

        var snapshotJson = JsonSerializer.Serialize(profile);
        var historyResult = history.AppendEdit(snapshotJson, new[] { "Preferences" });
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
