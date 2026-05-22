using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.AddFavoriteJob;

public class AddFavoriteJobCommandHandler : ICommandHandler<AddFavoriteJobCommand, Guid>
{
    private readonly IJobIndexEntryRepository _jobIndexRepo;
    private readonly IFavoriteJobRepository _favoriteRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AddFavoriteJobCommandHandler(
        IJobIndexEntryRepository jobIndexRepo,
        IFavoriteJobRepository favoriteRepo,
        IUnitOfWork unitOfWork)
    {
        _jobIndexRepo = jobIndexRepo;
        _favoriteRepo = favoriteRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddFavoriteJobCommand request, CancellationToken cancellationToken)
    {
        var exists = await _jobIndexRepo.ExistsAsync(request.PostingId, cancellationToken);
        if (!exists)
            return Result.Failure<Guid>(new Error("E-JOB-NOT-FOUND", "Job posting not found in index."));

        var existing = await _favoriteRepo.GetBySeekerAndPostingAsync(request.SeekerUserId, request.PostingId, cancellationToken);
        if (existing is not null)
            return Result.Success(existing.Id);

        var result = FavoriteJob.Add(request.SeekerUserId, request.PostingId, DateTime.UtcNow);
        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);

        await _favoriteRepo.AddAsync(result.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id);
    }
}
