using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RemoveCompanyImage;

public class RemoveCompanyImageCommandHandler : ICommandHandler<RemoveCompanyImageCommand>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCompanyImageCommandHandler(
        IEmployerProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveCompanyImageCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var image = profile.Images.FirstOrDefault(img => img.Id == request.CompanyImageId);
        if (image == null)
        {
            return Result.Failure(new Error("EmployerProfile.ImageNotFound", "Company image not found."));
        }

        var removeResult = profile.RemoveCompanyImage(request.CompanyImageId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
