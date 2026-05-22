using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Services;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RegeneratePublicSlug;

public class RegeneratePublicSlugCommandHandler : ICommandHandler<RegeneratePublicSlugCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RegeneratePublicSlugCommandHandler(
        IJobSeekerProfileRepository repository,
        IQrCodeGenerator qrCodeGenerator,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _qrCodeGenerator = qrCodeGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RegeneratePublicSlugCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var slugResult = PublicSlugGenerator.Generate(
            profile.Name,
            slug => _repository.IsSlugTakenAsync(slug, cancellationToken).GetAwaiter().GetResult());

        if (slugResult.IsFailure)
        {
            return Result.Failure(slugResult.Error);
        }

        var newSlug = slugResult.Value;
        var newPublicUrl = $"https://nexhire.com/p/{newSlug}";

        var qrCodeResult = await _qrCodeGenerator.GenerateAsync(newPublicUrl, cancellationToken);
        if (qrCodeResult.IsFailure)
        {
            return Result.Failure(qrCodeResult.Error);
        }

        var regenerateResult = profile.RegeneratePublicSlug(newSlug, qrCodeResult.Value);
        if (regenerateResult.IsFailure)
        {
            return regenerateResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
