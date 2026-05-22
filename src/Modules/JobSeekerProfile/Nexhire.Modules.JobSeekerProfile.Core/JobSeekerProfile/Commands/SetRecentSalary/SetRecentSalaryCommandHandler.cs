using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.SetRecentSalary;

public class SetRecentSalaryCommandHandler : ICommandHandler<SetRecentSalaryCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SetRecentSalaryCommandHandler(
        IJobSeekerProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetRecentSalaryCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        Money? recentSalary = null;
        if (request.Amount.HasValue)
        {
            var currency = request.Currency ?? "BDT";
            var moneyResult = Money.Create(request.Amount.Value, currency);
            if (moneyResult.IsFailure)
            {
                return Result.Failure(moneyResult.Error);
            }
            recentSalary = moneyResult.Value;
        }

        var result = profile.SetRecentSalary(recentSalary);
        if (result.IsFailure)
        {
            return result;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
