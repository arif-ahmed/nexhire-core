using MediatR;
using Nexhire.Modules.EmployerProfiles.Contracts.Events;
using Nexhire.Modules.EmployerProfiles.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.AddCandidateToShortlist;

public class AddCandidateToShortlistCommandHandler : ICommandHandler<AddCandidateToShortlistCommand>
{
    private readonly IEmployerProfileRepository _employerRepository;
    private readonly IShortlistRepository _shortlistRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public AddCandidateToShortlistCommandHandler(
        IEmployerProfileRepository employerRepository,
        IShortlistRepository shortlistRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _employerRepository = employerRepository;
        _shortlistRepository = shortlistRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(AddCandidateToShortlistCommand request, CancellationToken cancellationToken)
    {
        var profile = await _employerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var shortlist = await _shortlistRepository.GetByIdAsync(request.ShortlistId, cancellationToken);
        if (shortlist == null || shortlist.EmployerProfileId != profile.Id)
        {
            return Result.Failure(new Error("Shortlist.NotFound", "Shortlist not found."));
        }

        var addResult = shortlist.AddCandidate(request.CandidateUserId, request.MatchScore);
        if (addResult.IsFailure)
        {
            return addResult;
        }

        await _shortlistRepository.UpdateAsync(shortlist, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish integration event with correct BC-1 UserId
        var integrationEvent = new CandidateSavedToTalentPoolIntegrationEvent(
            Guid.NewGuid(),
            EmployerId: profile.UserId,
            JobSeekerId: request.CandidateUserId,
            PoolId: request.ShortlistId,
            At: DateTime.UtcNow,
            OccurredOnUtc: DateTime.UtcNow);

        await _publisher.Publish(integrationEvent, cancellationToken);

        return Result.Success();
    }
}
