using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RemoveEmployerDocument;

public class RemoveEmployerDocumentCommandHandler : ICommandHandler<RemoveEmployerDocumentCommand>
{
    private readonly IEmployerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveEmployerDocumentCommandHandler(
        IEmployerProfileRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveEmployerDocumentCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("EmployerProfile.NotFound", "Employer profile not found."));
        }

        var document = profile.Documents.FirstOrDefault(doc => doc.Id == request.DocumentId);
        if (document == null)
        {
            return Result.Failure(new Error("EmployerProfile.DocumentNotFound", "Supplementary document not found."));
        }

        var removeResult = profile.RemoveSupplementaryDocument(request.DocumentId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
