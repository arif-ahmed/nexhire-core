using Nexhire.Modules.JobSeekerProfile.Core.Domain.Events;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

public class ProfileHistory : AggregateRoot<Guid>
{
    private readonly List<ProfileVersion> _versions = new();

    public Guid JobSeekerProfileId { get; private set; }
    public IReadOnlyCollection<ProfileVersion> Versions => _versions.AsReadOnly();

    private ProfileHistory(Guid id, Guid jobSeekerProfileId) : base(id)
    {
        JobSeekerProfileId = jobSeekerProfileId;
    }

    private ProfileHistory()
    {
        // Required by EF Core
    }

    public static Result<ProfileHistory> Start(Guid id, Guid jobSeekerProfileId)
    {
        if (jobSeekerProfileId == Guid.Empty)
        {
            return Result.Failure<ProfileHistory>(new Error("ProfileHistory.InvalidProfileId", "JobSeekerProfileId cannot be empty."));
        }

        return Result.Success(new ProfileHistory(id, jobSeekerProfileId));
    }

    public Result AppendEdit(string snapshotJson, IEnumerable<string> changedFields)
    {
        var versionResult = ProfileVersion.Create(
            Guid.NewGuid(),
            snapshotJson,
            changedFields,
            HistoryAction.Edited,
            DateTime.UtcNow);

        if (versionResult.IsFailure)
        {
            return Result.Failure(versionResult.Error);
        }

        _versions.Add(versionResult.Value);
        return Result.Success();
    }

    public Result AppendRestore(string snapshotJson, Guid restoredFromVersionId)
    {
        var occurredOnUtc = DateTime.UtcNow;
        var versionResult = ProfileVersion.Create(
            Guid.NewGuid(),
            snapshotJson,
            new[] { "All (Restored)" },
            HistoryAction.Restored,
            occurredOnUtc);

        if (versionResult.IsFailure)
        {
            return Result.Failure(versionResult.Error);
        }

        _versions.Add(versionResult.Value);

        RaiseDomainEvent(new ProfileRestoredEvent(
            Guid.NewGuid(),
            JobSeekerProfileId,
            restoredFromVersionId,
            occurredOnUtc));

        return Result.Success();
    }

    public void PurgeOlderThan(DateTime cutoffUtc)
    {
        _versions.RemoveAll(v => v.CreatedOnUtc < cutoffUtc);
    }
}
