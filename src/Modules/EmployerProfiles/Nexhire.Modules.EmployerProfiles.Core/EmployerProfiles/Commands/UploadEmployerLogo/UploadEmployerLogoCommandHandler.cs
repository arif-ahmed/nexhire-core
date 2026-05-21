using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UploadEmployerLogo;

public class UploadEmployerLogoCommandHandler : ICommandHandler<UploadEmployerLogoCommand>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IObjectStorage _objectStorage;
    private readonly IVirusScanner _virusScanner;

    public UploadEmployerLogoCommandHandler(
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

    public async Task<Result> Handle(UploadEmployerLogoCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var storeResult = await _objectStorage.StoreAsync(request.Content, request.FileName, request.MimeType, cancellationToken);
        if (storeResult.IsFailure)
        {
            return Result.Failure(storeResult.Error);
        }

        var fileRef = storeResult.Value;

        var scanResult = await _virusScanner.ScanAsync(fileRef, cancellationToken);
        if (scanResult.Status == VirusScanStatus.Infected)
        {
            await _objectStorage.DeleteAsync(fileRef.StorageKey, cancellationToken);
            return Result.Failure(new Error("E-UPLOAD-VIRUS", "The uploaded file is infected."));
        }
        if (scanResult.Status == VirusScanStatus.Pending)
        {
            await _objectStorage.DeleteAsync(fileRef.StorageKey, cancellationToken);
            return Result.Failure(new Error("E-UPLOAD-PENDING", "The file virus scan is pending."));
        }

        var setLogoResult = profile.SetLogo(fileRef, scanResult);
        if (setLogoResult.IsFailure)
        {
            await _objectStorage.DeleteAsync(fileRef.StorageKey, cancellationToken);
            return setLogoResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
