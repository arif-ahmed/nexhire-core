using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

public class ProfileVersion : Entity<Guid>
{
    private readonly List<string> _changedFields = new();

    public string SnapshotJson { get; private set; } = null!;
    public IReadOnlyCollection<string> ChangedFields => _changedFields.AsReadOnly();
    public HistoryAction Action { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private ProfileVersion(
        Guid id,
        string snapshotJson,
        IEnumerable<string> changedFields,
        HistoryAction action,
        DateTime createdOnUtc) : base(id)
    {
        SnapshotJson = snapshotJson;
        _changedFields.AddRange(changedFields ?? Enumerable.Empty<string>());
        Action = action;
        CreatedOnUtc = createdOnUtc;
    }

    private ProfileVersion()
    {
        // Required by EF Core
    }

    public static Result<ProfileVersion> Create(
        Guid id,
        string snapshotJson,
        IEnumerable<string> changedFields,
        HistoryAction action,
        DateTime createdOnUtc)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return Result.Failure<ProfileVersion>(new Error("ProfileVersion.EmptySnapshot", "Snapshot JSON cannot be empty."));
        }

        return Result.Success(new ProfileVersion(id, snapshotJson, changedFields, action, createdOnUtc));
    }
}
