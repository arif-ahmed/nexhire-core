using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RemoveCandidateFromShortlist;

public class RemoveCandidateFromShortlistCommandHandler : ICommandHandler<RemoveCandidateFromShortlistCommand>
{
    private readonly IEmployerProfileRepository _employerRepository;
    private readonly IShortlistRepository _shortlistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCandidateFromShortlistCommandHandler(
        IEmployerProfileRepository employerRepository,
        IShortlistRepository shortlistRepository,
        IUnitOfWork unitOfWork)
    {
        _employerRepository = employerRepository;
        _shortlistRepository = shortlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveCandidateFromShortlistCommand request, CancellationToken cancellationToken)
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

        var removeResult = shortlist.RemoveCandidate(request.ShortlistMemberId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        await _shortlistRepository.UpdateAsync(shortlist, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
