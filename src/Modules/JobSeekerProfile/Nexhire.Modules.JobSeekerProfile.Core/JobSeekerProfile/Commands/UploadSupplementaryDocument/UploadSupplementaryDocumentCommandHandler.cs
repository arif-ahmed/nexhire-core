using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UploadSupplementaryDocument;

public class UploadSupplementaryDocumentCommandHandler : ICommandHandler<UploadSupplementaryDocumentCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IObjectStorage _objectStorage;
    private readonly IVirusScanner _virusScanner;
    private readonly IUnitOfWork _unitOfWork;

    public UploadSupplementaryDocumentCommandHandler(
        IJobSeekerProfileRepository repository,
        IObjectStorage objectStorage,
        IVirusScanner virusScanner,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _objectStorage = objectStorage;
        _virusScanner = virusScanner;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UploadSupplementaryDocumentCommand request, CancellationToken cancellationToken)
    {
        // 1. Load Profile
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        // 2. Validate Limit
        if (profile.Documents.Count >= 10)
        {
            return Result.Failure(new Error("E-UPLOAD-LIMIT-EXCEEDED", "Maximum limit of 10 supplementary documents has been exceeded."));
        }

        // 3. Parse DocumentKind
        if (!Enum.TryParse<DocumentKind>(request.Kind, true, out var kind))
        {
            return Result.Failure(new Error("SupplementaryDocument.InvalidKind", "Invalid document kind."));
        }

        // 4. Validate input variables
        var tempFileRefResult = FileReference.Create("temp_key", request.FileName, request.MimeType, request.Content.Length);
        if (tempFileRefResult.IsFailure)
        {
            return Result.Failure(tempFileRefResult.Error);
        }

        // 5. Store File
        var storeResult = await _objectStorage.StoreAsync(request.Content, request.FileName, request.MimeType, cancellationToken);
        if (storeResult.IsFailure)
        {
            return Result.Failure(storeResult.Error);
        }

        var fileRef = storeResult.Value;

        // 6. Scan File
        var scanResult = await _virusScanner.ScanAsync(fileRef, cancellationToken);
        if (scanResult.Status != VirusScanStatus.Clean)
        {
            await _objectStorage.DeleteAsync(fileRef.StorageKey, cancellationToken);
            return Result.Failure(new Error("E-UPLOAD-VIRUS", "Only clean files can be uploaded as supplementary documents."));
        }

        // 7. Add to Profile
        var addResult = profile.AddSupplementaryDocument(fileRef, kind, scanResult);
        if (addResult.IsFailure)
        {
            await _objectStorage.DeleteAsync(fileRef.StorageKey, cancellationToken);
            return addResult;
        }

        // 8. Save
        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
