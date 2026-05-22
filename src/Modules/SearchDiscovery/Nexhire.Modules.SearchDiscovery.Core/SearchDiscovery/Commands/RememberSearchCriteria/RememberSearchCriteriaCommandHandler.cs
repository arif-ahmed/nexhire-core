using Nexhire.Modules.SearchDiscovery.Core.Domain.Aggregates;
using Nexhire.Modules.SearchDiscovery.Core.Domain.Repositories;
using Nexhire.Modules.SearchDiscovery.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.SearchDiscovery.Core.SearchDiscovery.Commands.RememberSearchCriteria;

public class RememberSearchCriteriaCommandHandler : ICommandHandler<RememberSearchCriteriaCommand>
{
    private readonly ISearchSessionRepository _sessionRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RememberSearchCriteriaCommandHandler(ISearchSessionRepository sessionRepo, IUnitOfWork unitOfWork)
    {
        _sessionRepo = sessionRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RememberSearchCriteriaCommand request, CancellationToken cancellationToken)
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

        if (session.IsExpired(now))
            return Result.Failure(new Error("SearchSession.Expired", "Session has expired."));

        var criteriaResult = SearchCriteria.Create(keyword: request.Keyword, allowEmptyForPersistence: true);
        if (criteriaResult.IsFailure)
            return criteriaResult;

        session.RememberCriteria(criteriaResult.Value, now);
        await _sessionRepo.UpdateAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
