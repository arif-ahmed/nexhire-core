using System.Text.Json;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.RestoreProfileVersion;

public class RestoreProfileVersionCommandHandler : ICommandHandler<RestoreProfileVersionCommand>
{
    private readonly IJobSeekerProfileRepository _repository;
    private readonly IProfileHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreProfileVersionCommandHandler(
        IJobSeekerProfileRepository repository,
        IProfileHistoryRepository historyRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RestoreProfileVersionCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        var history = await _historyRepository.GetByProfileIdAsync(profile.Id, cancellationToken);
        if (history == null)
        {
            return Result.Failure(new Error("ProfileHistory.NotFound", "Profile history not found."));
        }

        var version = history.Versions.FirstOrDefault(v => v.Id == request.VersionId);
        if (version == null)
        {
            return Result.Failure(new Error("ProfileVersion.NotFound", "Profile version not found."));
        }

        ProfileSnapshotDto? snapshot;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            snapshot = JsonSerializer.Deserialize<ProfileSnapshotDto>(version.SnapshotJson, options);
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("ProfileVersion.DeserializationFailed", $"Failed to deserialize snapshot: {ex.Message}"));
        }

        if (snapshot == null)
        {
            return Result.Failure(new Error("ProfileVersion.NullSnapshot", "Deserialized snapshot is null."));
        }

        profile.RestoreSnapshot(
            snapshot.Name,
            snapshot.Email,
            snapshot.Mobile,
            snapshot.Gender,
            snapshot.Education,
            snapshot.Experience,
            snapshot.Skills,
            snapshot.Documents,
            snapshot.Preferences,
            snapshot.CurrentAddress,
            snapshot.PermanentAddress,
            snapshot.RecentSalary,
            snapshot.Visibility,
            snapshot.PublicSharing,
            snapshot.Verification,
            snapshot.HasActiveResume);

        var newSnapshotJson = JsonSerializer.Serialize(profile);
        var restoreResult = history.AppendRestore(newSnapshotJson, request.VersionId);
        if (restoreResult.IsFailure)
        {
            return restoreResult;
        }

        await _repository.UpdateAsync(profile, cancellationToken);
        await _historyRepository.UpdateAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private class ProfileSnapshotDto
    {
        public PersonName Name { get; set; } = null!;
        public EmailAddress Email { get; set; } = null!;
        public MobileNumber Mobile { get; set; } = null!;
        public Gender Gender { get; set; }
        public List<EducationEntry> Education { get; set; } = new();
        public List<ExperienceEntry> Experience { get; set; } = new();
        public List<ProfileSkill> Skills { get; set; } = new();
        public List<SupplementaryDocument> Documents { get; set; } = new();
        public JobPreferences? Preferences { get; set; }
        public Address? CurrentAddress { get; set; }
        public Address? PermanentAddress { get; set; }
        public Money? RecentSalary { get; set; }
        public ProfileVisibility Visibility { get; set; }
        public PublicSharingSettings PublicSharing { get; set; } = null!;
        public VerificationFlags Verification { get; set; } = null!;
        public bool HasActiveResume { get; set; }
    }
}
