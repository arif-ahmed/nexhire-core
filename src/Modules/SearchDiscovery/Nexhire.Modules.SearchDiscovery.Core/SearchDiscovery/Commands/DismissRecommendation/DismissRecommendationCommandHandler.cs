using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.DismissRecommendation;

public class DismissRecommendationCommandHandler : ICommandHandler<DismissRecommendationCommand>
{
    private readonly ISearchSessionRepository _sessionRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DismissRecommendationCommandHandler(ISearchSessionRepository sessionRepo, IUnitOfWork unitOfWork)
    {
        _sessionRepo = sessionRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DismissRecommendationCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepo.GetBySeekerAsync(request.SeekerUserId, cancellationToken);
        var now = DateTime.UtcNow;

        if (session is null)
        {
            var startResult = SearchSession.Start(request.SeekerUserId, now);
            if (startResult.IsFailure)
                return startResult;
            session = startResult.Value;
            await _sessionRepo.AddAsync(session, cancellationToken);
        }

        session.DismissRecommendation(request.PostingId, now);
        await _sessionRepo.UpdateAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
