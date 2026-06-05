using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Services;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.DTOs;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.EnablePublicSharing;

public class EnablePublicSharingCommandHandler : ICommandHandler<EnablePublicSharingCommand, PublicSharingSettingsDto>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public EnablePublicSharingCommandHandler(
        IJobSeekerProfileRepository repository,
        IQrCodeGenerator qrCodeGenerator,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _qrCodeGenerator = qrCodeGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PublicSharingSettingsDto>> Handle(EnablePublicSharingCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure<PublicSharingSettingsDto>(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var slugResult = PublicSlugGenerator.Generate(
            profile.Name,
            slug => _repository.IsSlugTakenAsync(slug, cancellationToken).GetAwaiter().GetResult());

        if (slugResult.IsFailure)
        {
            return Result.Failure<PublicSharingSettingsDto>(slugResult.Error);
        }

        var slug = slugResult.Value;
        var publicUrl = $"https://nexhire.com/p/{slug}";

        var qrCodeResult = await _qrCodeGenerator.GenerateAsync(publicUrl, cancellationToken);
        if (qrCodeResult.IsFailure)
        {
            return Result.Failure<PublicSharingSettingsDto>(qrCodeResult.Error);
        }

        var enableResult = profile.EnablePublicSharing(slug, qrCodeResult.Value);
        if (enableResult.IsFailure)
        {
            return Result.Failure<PublicSharingSettingsDto>(enableResult.Error);
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(profile.PublicSharing.ToDto());
    }
}
