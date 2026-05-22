using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.DeleteSupplementaryDocument;

public class DeleteSupplementaryDocumentCommandHandler : ICommandHandler<DeleteSupplementaryDocumentCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IObjectStorage _objectStorage;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSupplementaryDocumentCommandHandler(
        IJobSeekerProfileRepository repository,
        IObjectStorage objectStorage,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _objectStorage = objectStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteSupplementaryDocumentCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var doc = profile.Documents.FirstOrDefault(d => d.Id == request.DocumentId);
        if (doc == null)
        {
            return Result.Failure(new Error("SupplementaryDocument.NotFound", "Supplementary document not found."));
        }

        var storageKey = doc.File.StorageKey;

        var removeResult = profile.RemoveSupplementaryDocument(request.DocumentId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        await _objectStorage.DeleteAsync(storageKey, cancellationToken);

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
