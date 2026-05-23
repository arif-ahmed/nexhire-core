using System.Text.Json;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Events;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.SyncRecord;

public enum SyncDirection { Inbound, Outbound }
public enum SyncStatus { Accepted, Quarantined, Synced, Failed, Archived }
public enum AttemptOutcome { Success, TransientFailure, PermanentFailure }

public sealed class SyncRecord : AggregateRoot<Guid>
{
    private readonly List<SyncAttempt> _attempts = new();

    public SyncDirection Direction { get; private set; }
    public Guid? PartnerId { get; private set; }
    public Guid? ConnectorId { get; private set; }
    public ExternalRef ExternalRef { get; private set; } = null!;
    public Guid? InternalJobId { get; private set; }
    public SyncStatus Status { get; private set; }
    public string RawPayload { get; private set; } = null!;
    public string? NormalisedSnapshot { get; private set; }
    public IReadOnlyCollection<SyncAttempt> Attempts => _attempts.AsReadOnly();
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime LastSyncOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private SyncRecord() { }

    private SyncRecord(
        Guid id, 
        SyncDirection direction, 
        ExternalRef externalRef, 
        string rawPayload, 
        Guid? partnerId, 
        Guid? connectorId, 
        Guid? internalJobId, 
        DateTime createdOnUtc) : base(id)
    {
        Direction = direction;
        ExternalRef = externalRef;
        RawPayload = rawPayload;
        PartnerId = partnerId;
        ConnectorId = connectorId;
        InternalJobId = internalJobId;
        Status = SyncStatus.Accepted;
        CreatedOnUtc = createdOnUtc;
        LastSyncOnUtc = createdOnUtc;
    }

    public static Result<SyncRecord> StartInbound(ExternalRef externalRef, string rawPayload, Guid? partnerId = null, Guid? connectorId = null)
    {
        if (externalRef == null)
            return Result.Failure<SyncRecord>(new Error("SyncRecord.RefRequired", "External reference is required."));
        if (string.IsNullOrWhiteSpace(rawPayload))
            return Result.Failure<SyncRecord>(new Error("SyncRecord.PayloadRequired", "Raw payload is required."));
        if (partnerId == null && connectorId == null)
            return Result.Failure<SyncRecord>(new Error("SyncRecord.IdentityRequired", "Either PartnerId or ConnectorId must be set."));
        if (partnerId != null && connectorId != null)
            return Result.Failure<SyncRecord>(new Error("SyncRecord.IdentityConflict", "Cannot set both PartnerId and ConnectorId."));

        return Result.Success(new SyncRecord(Guid.NewGuid(), SyncDirection.Inbound, externalRef, rawPayload, partnerId, connectorId, null, DateTime.UtcNow));
    }

    public static Result<SyncRecord> StartOutbound(ExternalRef externalRef, Guid internalJobId, Guid connectorId)
    {
        if (externalRef == null)
            return Result.Failure<SyncRecord>(new Error("SyncRecord.RefRequired", "External reference is required."));
        if (internalJobId == Guid.Empty)
            return Result.Failure<SyncRecord>(new Error("SyncRecord.InternalJobRequired", "Internal job ID is required."));
        if (connectorId == Guid.Empty)
            return Result.Failure<SyncRecord>(new Error("SyncRecord.ConnectorRequired", "Connector ID is required."));

        return Result.Success(new SyncRecord(Guid.NewGuid(), SyncDirection.Outbound, externalRef, "{}", null, connectorId, internalJobId, DateTime.UtcNow));
    }

    public Result RecordNormalised(NormalisedJobPosting normalisedSnapshot)
    {
        if (Status != SyncStatus.Accepted)
            return Result.Failure(new Error("SyncRecord.InvalidState", "Snapshot can only be recorded in Accepted state."));

        NormalisedSnapshot = JsonSerializer.Serialize(normalisedSnapshot);
        LastSyncOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkSynced(Guid? internalJobId = null)
    {
        if (Status is not (SyncStatus.Accepted or SyncStatus.Synced or SyncStatus.Failed or SyncStatus.Quarantined))
            return Result.Failure(new Error("SyncRecord.InvalidState", "Cannot sync from current state."));

        Status = SyncStatus.Synced;
        if (internalJobId != null)
        {
            InternalJobId = internalJobId;
        }

        ErrorCode = null;
        ErrorMessage = null;
        LastSyncOnUtc = DateTime.UtcNow;

        if (Direction == SyncDirection.Inbound && !string.IsNullOrEmpty(NormalisedSnapshot))
        {
            var posting = JsonSerializer.Deserialize<NormalisedJobPosting>(NormalisedSnapshot);
            if (posting != null)
            {
                RaiseDomainEvent(new ExternalJobIngestedIntegrationEvent(ExternalRef, PartnerId, posting, LastSyncOnUtc));
            }
        }

        return Result.Success();
    }

    public Result MarkUpdated(List<string> changedFields)
    {
        if (Status != SyncStatus.Synced)
            return Result.Failure(new Error("SyncRecord.NotSynced", "Can only update an already synced record."));

        LastSyncOnUtc = DateTime.UtcNow;

        if (Direction == SyncDirection.Inbound && !string.IsNullOrEmpty(NormalisedSnapshot))
        {
            var posting = JsonSerializer.Deserialize<NormalisedJobPosting>(NormalisedSnapshot);
            if (posting != null)
            {
                RaiseDomainEvent(new ExternalJobUpdatedIntegrationEvent(ExternalRef, PartnerId, changedFields, posting, LastSyncOnUtc));
            }
        }

        return Result.Success();
    }

    public Result MarkRetracted()
    {
        if (Status != SyncStatus.Synced)
            return Result.Failure(new Error("SyncRecord.NotSynced", "Can only retract an already synced record."));

        Status = SyncStatus.Archived;
        LastSyncOnUtc = DateTime.UtcNow;

        if (Direction == SyncDirection.Inbound)
        {
            RaiseDomainEvent(new ExternalJobRetractedIntegrationEvent(ExternalRef, PartnerId, LastSyncOnUtc));
        }

        return Result.Success();
    }

    public Result Quarantine(string errorCode, string errorMessage)
    {
        if (Status != SyncStatus.Accepted)
            return Result.Failure(new Error("SyncRecord.InvalidState", "Can only quarantine from Accepted state."));

        Status = SyncStatus.Quarantined;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        LastSyncOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new SyncErrorDetectedIntegrationEvent(PartnerId, ConnectorId, "Quarantine", Id, LastSyncOnUtc));
        return Result.Success();
    }

    public Result RecordAttempt(Guid attemptId, AttemptOutcome outcome, int? responseCode, string? detail, int processingMs, bool wasManualOverride = false)
    {
        int attemptNo = _attempts.Count + 1;
        var attempt = new SyncAttempt(attemptId, attemptNo, outcome, responseCode, detail, processingMs, wasManualOverride);
        _attempts.Add(attempt);
        LastSyncOnUtc = DateTime.UtcNow;

        if (outcome == AttemptOutcome.PermanentFailure || (outcome == AttemptOutcome.TransientFailure && _attempts.Count >= 3))
        {
            Status = SyncStatus.Failed;
            ErrorCode = outcome == AttemptOutcome.PermanentFailure ? "PERMANENT_FAILURE" : "RETRY_LIMIT_EXCEEDED";
            ErrorMessage = detail ?? "Maximum sync attempts reached without success.";
            RaiseDomainEvent(new SyncErrorDetectedIntegrationEvent(PartnerId, ConnectorId, "Failure", Id, LastSyncOnUtc));
        }

        return Result.Success();
    }

    public Result Retry()
    {
        if (Status is not (SyncStatus.Failed or SyncStatus.Quarantined))
            return Result.Failure(new Error("SyncRecord.CannotRetry", "Can only retry a failed or quarantined record."));

        Status = SyncStatus.Accepted;
        ErrorCode = null;
        ErrorMessage = null;
        LastSyncOnUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result ManualOverride(NormalisedJobPosting correctedPayload, Guid engineerId)
    {
        if (Status is not (SyncStatus.Failed or SyncStatus.Quarantined))
            return Result.Failure(new Error("SyncRecord.CannotOverride", "Can only override a failed or quarantined record."));

        NormalisedSnapshot = JsonSerializer.Serialize(correctedPayload);
        LastSyncOnUtc = DateTime.UtcNow;

        // Record a manual attempt
        var attemptId = Guid.NewGuid();
        RecordAttempt(attemptId, AttemptOutcome.Success, 200, $"Manual override applied by Engineer: {engineerId}", 0, true);

        // Transition to synced
        MarkSynced(InternalJobId);

        RaiseDomainEvent(new SyncReconciledIntegrationEvent(PartnerId, ConnectorId, 1, LastSyncOnUtc));
        return Result.Success();
    }

    public Result Archive()
    {
        Status = SyncStatus.Archived;
        LastSyncOnUtc = DateTime.UtcNow;
        return Result.Success();
    }
}

public sealed class SyncAttempt : Entity<Guid>
{
    public int AttemptNo { get; private set; }
    public AttemptOutcome Outcome { get; private set; }
    public int? ResponseCode { get; private set; }
    public string? Detail { get; private set; }
    public int ProcessingMs { get; private set; }
    public DateTime AttemptedOnUtc { get; private set; }
    public bool WasManualOverride { get; private set; }

    private SyncAttempt() { }

    internal SyncAttempt(
        Guid id, 
        int attemptNo, 
        AttemptOutcome outcome, 
        int? responseCode, 
        string? detail, 
        int processingMs, 
        bool wasManualOverride) : base(id)
    {
        AttemptNo = attemptNo;
        Outcome = outcome;
        ResponseCode = responseCode;
        Detail = detail;
        ProcessingMs = processingMs;
        WasManualOverride = wasManualOverride;
        AttemptedOnUtc = DateTime.UtcNow;
    }
}
