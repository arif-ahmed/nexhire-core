using Nexhire.Modules.Reporting.Core.Domain.Enums;
using Nexhire.Modules.Reporting.Core.Domain.Events;
using Nexhire.Modules.Reporting.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.Reporting.Core.Domain.Aggregates;

public class RetentionPolicyVersion : Entity<Guid>
{
    public Guid PolicyId { get; private set; }
    public int VersionNumber { get; private set; }
    public RetentionScope Scope { get; private set; } = null!;
    public int RetentionDays { get; private set; }
    public RetentionAction Action { get; private set; }
    public int WarningDays { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public Guid ChangedBy { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private RetentionPolicyVersion() { }
    internal RetentionPolicyVersion(Guid id, Guid policyId, int versionNumber, RetentionScope scope,
        int retentionDays, RetentionAction action, int warningDays, DateTime effectiveFrom, Guid changedBy) : base(id)
    {
        PolicyId = policyId; VersionNumber = versionNumber; Scope = scope;
        RetentionDays = retentionDays; Action = action; WarningDays = warningDays;
        EffectiveFromUtc = effectiveFrom; ChangedBy = changedBy; CreatedOnUtc = DateTime.UtcNow;
    }
}

public class RetentionRun : Entity<Guid>
{
    public Guid PolicyId { get; private set; }
    public int PolicyVersionNumber { get; private set; }
    public int RecordsAffected { get; private set; }
    public RetentionAction ActionTaken { get; private set; }
    public DateTime CutoffUtc { get; private set; }
    public DateTime ExecutedOnUtc { get; private set; }

    private RetentionRun() { }
    internal RetentionRun(Guid id, Guid policyId, int policyVersionNumber, int recordsAffected,
        RetentionAction actionTaken, DateTime cutoffUtc, DateTime executedOnUtc) : base(id)
    {
        PolicyId = policyId; PolicyVersionNumber = policyVersionNumber;
        RecordsAffected = recordsAffected; ActionTaken = actionTaken;
        CutoffUtc = cutoffUtc; ExecutedOnUtc = executedOnUtc;
    }
}

public class RetentionPolicy : AggregateRoot<Guid>
{
    private readonly List<RetentionPolicyVersion> _versions = new();
    private readonly List<RetentionRun> _runs = new();

    public string Name { get; private set; } = null!;
    public RetentionScope Scope { get; private set; } = null!;
    public int RetentionDays { get; private set; }
    public RetentionAction Action { get; private set; }
    public int WarningDays { get; private set; }
    public int CurrentVersionNumber { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public RetentionPolicyStatus Status { get; private set; }
    public IReadOnlyCollection<RetentionPolicyVersion> Versions => _versions.AsReadOnly();
    public IReadOnlyCollection<RetentionRun> Runs => _runs.AsReadOnly();
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private RetentionPolicy() { }

    public static Result<RetentionPolicy> Create(string name, RetentionScope scope, int retentionDays, RetentionAction action, int warningDays)
    {
        if (retentionDays <= 0) return Result.Failure<RetentionPolicy>(new Error("RetentionPolicy.InvalidDays", "RetentionDays must be > 0."));
        if (warningDays < 0) return Result.Failure<RetentionPolicy>(new Error("RetentionPolicy.InvalidWarning", "WarningDays must be >= 0."));

        var now = DateTime.UtcNow;
        var policy = new RetentionPolicy
        {
            Id = Guid.NewGuid(), Name = name, Scope = scope, RetentionDays = retentionDays,
            Action = action, WarningDays = warningDays, CurrentVersionNumber = 1,
            EffectiveFromUtc = now, Status = RetentionPolicyStatus.Active,
            CreatedOnUtc = now, UpdatedOnUtc = now
        };
        policy._versions.Add(new RetentionPolicyVersion(Guid.NewGuid(), policy.Id, 1, scope, retentionDays, action, warningDays, now, Guid.Empty));
        return Result.Success(policy);
    }

    public Result Revise(RetentionScope scope, int retentionDays, RetentionAction action, int warningDays, Guid changedBy, DateTime effectiveFromUtc)
    {
        if (retentionDays <= 0) return Result.Failure(new Error("RetentionPolicy.InvalidDays", "RetentionDays must be > 0."));
        if (effectiveFromUtc < DateTime.UtcNow.AddSeconds(-5))
            return Result.Failure(new Error("RetentionPolicy.PastEffectiveDate", "EffectiveFromUtc must be >= now."));

        CurrentVersionNumber++;
        Scope = scope; RetentionDays = retentionDays; Action = action; WarningDays = warningDays;
        EffectiveFromUtc = effectiveFromUtc; UpdatedOnUtc = DateTime.UtcNow;
        _versions.Add(new RetentionPolicyVersion(Guid.NewGuid(), Id, CurrentVersionNumber, scope, retentionDays, action, warningDays, effectiveFromUtc, changedBy));
        RaiseDomainEvent(new RetentionPolicyRevised(Guid.NewGuid(), Id, CurrentVersionNumber, UpdatedOnUtc));
        return Result.Success();
    }

    public Result RecordRun(int policyVersionNumber, DateTime cutoffUtc, int recordsAffected, RetentionAction actionTaken, DateTime executedOnUtc)
    {
        if (Status == RetentionPolicyStatus.Archived)
            return Result.Failure(new Error("RetentionPolicy.Archived", "Archived policies cannot run."));
        var run = new RetentionRun(Guid.NewGuid(), Id, policyVersionNumber, recordsAffected, actionTaken, cutoffUtc, executedOnUtc);
        _runs.Add(run);
        RaiseDomainEvent(new RetentionApplied(Guid.NewGuid(), Id, run.Id, recordsAffected, actionTaken.ToString(), executedOnUtc));
        return Result.Success();
    }

    public Result Archive()
    {
        if (Status != RetentionPolicyStatus.Active)
            return Result.Failure(new Error("RetentionPolicy.NotActive", "Only active policies can be archived."));
        Status = RetentionPolicyStatus.Archived;
        UpdatedOnUtc = DateTime.UtcNow;
        return Result.Success();
    }
}
