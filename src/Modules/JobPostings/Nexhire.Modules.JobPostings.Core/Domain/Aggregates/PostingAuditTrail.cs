using Nexhire.Modules.JobPostings.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobPostings.Core.Domain.Aggregates;

public sealed class PostingAuditTrail : AggregateRoot<Guid>
{
    private readonly List<AuditEntry> _entries = new();

    public Guid JobPostingId { get; private set; }
    public IReadOnlyCollection<AuditEntry> Entries => _entries.AsReadOnly();

    private PostingAuditTrail() { }

    private PostingAuditTrail(Guid id, Guid jobPostingId) : base(id)
    {
        JobPostingId = jobPostingId;
    }

    public static PostingAuditTrail Create(Guid jobPostingId) => new(Guid.NewGuid(), jobPostingId);

    public Result RecordStatusChange(AuditActor actor, StatusTransition transition, string? reason, DateTime? occurredOnUtc = null)
    {
        if ((transition.To is PostingStatus.Suspended or PostingStatus.Removed) && string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(new Error("E-POST-REASON-REQUIRED", "A reason is required for suspension or removal."));
        }

        _entries.Add(AuditEntry.StatusChange(Guid.NewGuid(), actor, transition, reason, occurredOnUtc ?? DateTime.UtcNow));
        return Result.Success();
    }

    public Result RecordFieldEdit(AuditActor actor, IReadOnlyCollection<string> changedFields, DateTime? occurredOnUtc = null)
    {
        if (changedFields.Count == 0)
        {
            return Result.Success();
        }

        _entries.Add(AuditEntry.FieldEdit(Guid.NewGuid(), actor, changedFields, occurredOnUtc ?? DateTime.UtcNow));
        return Result.Success();
    }
}

public sealed class AuditEntry : Entity<Guid>
{
    public AuditEntryKind Kind { get; private set; }
    public AuditActor Actor { get; private set; } = null!;
    public StatusTransition? StatusTransition { get; private set; }
    public IReadOnlyCollection<string> ChangedFields { get; private set; } = Array.Empty<string>();
    public string? Reason { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }

    private AuditEntry() { }

    private AuditEntry(Guid id) : base(id) { }

    public static AuditEntry StatusChange(Guid id, AuditActor actor, StatusTransition transition, string? reason, DateTime occurredOnUtc) =>
        new(id)
        {
            Kind = AuditEntryKind.StatusChange,
            Actor = actor,
            StatusTransition = transition,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            OccurredOnUtc = TruncateToSecond(occurredOnUtc)
        };

    public static AuditEntry FieldEdit(Guid id, AuditActor actor, IReadOnlyCollection<string> changedFields, DateTime occurredOnUtc) =>
        new(id)
        {
            Kind = AuditEntryKind.FieldEdit,
            Actor = actor,
            ChangedFields = changedFields.ToArray(),
            OccurredOnUtc = TruncateToSecond(occurredOnUtc)
        };

    private static DateTime TruncateToSecond(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
}
