using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UploadCompanyImage;

public class UploadCompanyImageCommandHandler : ICommandHandler<UploadCompanyImageCommand, Guid>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IObjectStorage _objectStorage;
    private readonly IVirusScanner _virusScanner;

    public UploadCompanyImageCommandHandler(
        IEmployerProfileRepository repository,
        IUnitOfWork unitOfWork,
        IObjectStorage objectStorage,
        IVirusScanner virusScanner)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _objectStorage = objectStorage;
        _virusScanner = virusScanner;
    }

    public async Task<Result<Guid>> Handle(UploadCompanyImageCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<Guid>(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var storeResult = await _objectStorage.StoreAsync(request.Content, request.FileName, request.MimeType, cancellationToken);
        if (storeResult.IsFailure)
        {
            return Result.Failure<Guid>(storeResult.Error);
        }

        var fileRef = storeResult.Value;

        var scanResult = await _virusScanner.ScanAsync(fileRef, cancellationToken);
        if (scanResult.Status == VirusScanStatus.Infected)
        {
            await _objectStorage.DeleteAsync(fileRef.StorageKey, cancellationToken);
            return Result.Failure<Guid>(new Error("E-UPLOAD-VIRUS", "The uploaded image is infected."));
        }
        if (scanResult.Status == VirusScanStatus.Pending)
        {
            await _objectStorage.DeleteAsync(fileRef.StorageKey, cancellationToken);
            return Result.Failure<Guid>(new Error("E-UPLOAD-PENDING", "The image virus scan is pending."));
        }

        var addImgResult = profile.AddCompanyImage(fileRef, scanResult);
        if (addImgResult.IsFailure)
        {
            await _objectStorage.DeleteAsync(fileRef.StorageKey, cancellationToken);
            return Result.Failure<Guid>(addImgResult.Error);
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(addImgResult.Value);
    }
}
