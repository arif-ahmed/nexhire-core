using System.Text.Json;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Services;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.ConfirmParsedResumeFields;

public class ConfirmParsedResumeFieldsCommandHandler : ICommandHandler<ConfirmParsedResumeFieldsCommand>
{
    private readonly IJobSeekerProfileRepository _profileRepository;
    private readonly IResumeRepository _resumeRepository;
    private readonly IProfileHistoryRepository _historyRepository;
    private readonly ITaxonomyApi _taxonomyApi;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmParsedResumeFieldsCommandHandler(
        IJobSeekerProfileRepository profileRepository,
        IResumeRepository resumeRepository,
        IProfileHistoryRepository historyRepository,
        ITaxonomyApi taxonomyApi,
        IUnitOfWork unitOfWork)
    {
        _profileRepository = profileRepository;
        _resumeRepository = resumeRepository;
        _historyRepository = historyRepository;
        _taxonomyApi = taxonomyApi;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ConfirmParsedResumeFieldsCommand request, CancellationToken cancellationToken)
    {
        // 1. Load profile
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        // 2. Load history
        var history = await _historyRepository.GetByProfileIdAsync(profile.Id, cancellationToken);
        if (history == null)
        {
            return Result.Failure(new Error("ProfileHistory.NotFound", "Profile history not found."));
        }

        // 3. Load resume
        var resume = await _resumeRepository.GetByIdAsync(request.ResumeId, cancellationToken);
        if (resume == null)
        {
            return Result.Failure(new Error("Resume.NotFound", "Resume not found."));
        }

        if (resume.ProfileId != profile.Id)
        {
            return Result.Failure(new Error("Resume.ProfileMismatch", "Resume does not belong to the job seeker profile."));
        }

        if (resume.ParseStatus != ResumeParseStatus.Parsed)
        {
            return Result.Failure(new Error("Resume.InvalidStatusForMerge", "Only successfully parsed resumes can be merged."));
        }

        if (resume.ParsedData == null)
        {
            return Result.Failure(new Error("Resume.NullParsedData", "Parsed resume data is missing."));
        }

        // 4. Map skills in parallel/sequence before merging
        var skillMappings = new Dictionary<string, Result<CanonicalSkillRef>>(StringComparer.OrdinalIgnoreCase);
        if (resume.ParsedData.Skills != null)
        {
            foreach (var skill in resume.ParsedData.Skills)
            {
                var mapResult = await _taxonomyApi.MapSkillAsync(skill.RawLabel, cancellationToken);
                skillMappings[skill.RawLabel] = mapResult;
            }
        }

        // 5. Merge selected fields
        var mergeResult = ResumeToProfileMerger.MergeSelectedFields(
            profile,
            resume.ParsedData,
            request.SelectedFieldKeys,
            label => skillMappings.TryGetValue(label, out var res) ? res : Result.Failure<CanonicalSkillRef>(new Error("Taxonomy.MappingFailed", $"Failed to map skill '{label}'")));

        if (mergeResult.IsFailure)
        {
            return mergeResult;
        }

        // 6. Confirm resume fields and mark resume active on profile
        var confirmResult = resume.ConfirmMergedFields(request.SelectedFieldKeys);
        if (confirmResult.IsFailure)
        {
            return confirmResult;
        }

        profile.MarkResumeAttached();

        // 7. Record History Snapshot
        var snapshotJson = JsonSerializer.Serialize(profile);
        var historyResult = history.AppendEdit(snapshotJson, new[] { "ResumeMerge" });
        if (historyResult.IsFailure)
        {
            return historyResult;
        }

        // 8. Persist
        await _profileRepository.UpdateAsync(profile, cancellationToken);
        await _resumeRepository.UpdateAsync(resume, cancellationToken);
        await _historyRepository.UpdateAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
