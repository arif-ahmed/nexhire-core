using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.CreateShortlist;

public class CreateShortlistCommandHandler : ICommandHandler<CreateShortlistCommand, Guid>
{
    private readonly IEmployerProfileRepository _employerRepository;
    private readonly IShortlistRepository _shortlistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateShortlistCommandHandler(
        IEmployerProfileRepository employerRepository,
        IShortlistRepository shortlistRepository,
        IUnitOfWork unitOfWork)
    {
        _employerRepository = employerRepository;
        _shortlistRepository = shortlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateShortlistCommand request, CancellationToken cancellationToken)
    {
        var profile = await _employerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<Guid>(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var shortlistResult = Shortlist.Create(Guid.NewGuid(), profile.Id, request.Name);
        if (shortlistResult.IsFailure)
        {
            return Result.Failure<Guid>(shortlistResult.Error);
        }

        var shortlist = shortlistResult.Value;
        await _shortlistRepository.AddAsync(shortlist, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(shortlist.Id);
    }
}
